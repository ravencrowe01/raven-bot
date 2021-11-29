#region copyright
/** Raven Bot, a light-weight Discord bot using DSharp+ for gateway and command handling.
 *  Copyright (C) 2021 Raven Crowe
 *  
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *  
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Affero General Public License for more details.
 *  
 *  You should have received a copy of the GNU Affero General Public License
 *  along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
#endregion

using Microsoft.Extensions.DependencyInjection;
using RavenBot.Required;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RavenBot.External.Services {
    public class RequiredServicesProvider : IRequiredServicesProvider {
        public List<RequiredService> RequiredSingletonServices { get; } = new ();
        public List<RequiredService> RequiredScopedServices { get; } = new ();
        public List<RequiredService> RequiredTransientServices { get; } = new ();

        public IServiceCollection LoadRequiredServices (string workingDirectory) {
            if (!Directory.Exists (workingDirectory)) {
                throw new IOException ($"The directory, {workingDirectory}, does not exist.");
            }

            List<Assembly> assemblies = LoadAssemblies (workingDirectory);

            List<IRequiredServices> requiredServices = LoadRequiredServices (assemblies);

            if (requiredServices.Count < 1) {
                return null;
            }

            return BuildServiceCollection (requiredServices);
        }

        private static List<Assembly> LoadAssemblies (string workingDirectory) {
            var assemblyPaths = Directory.GetFiles (workingDirectory, "*.dll").ToList ();

            var assemblies = new List<Assembly> ();

            foreach (var assemblyPath in assemblyPaths) {
                var asm = Assembly.LoadFrom (assemblyPath);
                assemblies.Add (asm);
            }

            return assemblies;
        }

        private static IList<Type> GetServiceConfigurations (IEnumerable<Assembly> commandAssemblies) => (from asm in commandAssemblies
                                                                                                          from type in asm.GetTypes ()
                                                                                                          where type.IsAssignableTo (typeof (IRequiredServices))
                                                                                                          select type).ToList ();

        private static List<IRequiredServices> LoadRequiredServices (List<Assembly> assemblies) {
            var requiredServicesConfigurations = GetServiceConfigurations (assemblies);

            var requiredServices = new List<IRequiredServices> ();

            foreach (var config in requiredServicesConfigurations) {
                if (config.GetConstructors ().Any (c => c.GetParameters ().Length == 0)) {
                    requiredServices.Add (Activator.CreateInstance (config) as IRequiredServices);
                }
            }

            return requiredServices;
        }

        private static void AddSingletonServices (IRequiredServices service, IServiceCollection serviceCollection) {
            var singletons = service.RequiredSingletons;

            if (singletons?.Count > 0) {
                singletons.ForEach (s => serviceCollection.AddSingleton (s.Service, s.Implementation));
            }
        }

        private static void AddScopedServices (IRequiredServices service, IServiceCollection serviceCollection) {
            var scoped = service.RequiredScoped;

            if (scoped?.Count > 0) {
                scoped.ForEach (s => serviceCollection.AddScoped (s.Service, s.Implementation));
            }
        }

        private static void AddTransientServices (IRequiredServices service, IServiceCollection serviceCollection) {
            var transients = service.RequiredTransient;

            if (transients?.Count > 0) {
                transients.ForEach (s => serviceCollection.AddTransient (s.Service, s.Implementation));
            }
        }

        private static IServiceCollection BuildServiceCollection (List<IRequiredServices> requiredServices) {
            var serviceCollection = new ServiceCollection ();

            foreach (var service in requiredServices) {
                AddSingletonServices (service, serviceCollection);

                AddScopedServices (service, serviceCollection);

                AddTransientServices (service, serviceCollection);
            }

            return serviceCollection;
        }
    }
}
