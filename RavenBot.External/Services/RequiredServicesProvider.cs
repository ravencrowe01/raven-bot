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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RavenBot.External.Services {
    public class RequiredServicesProvider : IRequiredServicesProvider {
        public List<Type> RequiredSingletonServices { get; }
        public List<Type> RequiredScopedServices { get; }
        public List<Type> RequiredTransientServices { get; }

        public RequiredServicesProvider (IReadOnlyCollection<Assembly> commandAssemblies) {
            GetRequiredServices(commandAssemblies);
        }

        private void GetRequiredServices (IReadOnlyCollection<Assembly> commandAssemblies) {
            var requiredServicesConfigurations = GetServiceConfigurations();

            GetRequiredSingletons();
            GetRequiredScoped();
            GetRequiredTransient();

            IReadOnlyCollection<Type> GetServiceConfigurations ()
            {
                return (from asm in commandAssemblies 
                        from type in asm.ExportedTypes 
                        where type.IsAssignableTo(typeof(IRequiredServicesProvider))
                        select type).ToList();
            }

            void GetRequiredSingletons () {
                var singletons = new List<Type>();
                LoadRequiredSingletons();

                if (singletons?.Count > 0) {
                    foreach (var type in singletons) {
                        RequiredSingletonServices.Add(type);
                    }
                }

                void LoadRequiredSingletons () {
                    foreach (var config in requiredServicesConfigurations) {
                        var requiredServices = Activator.CreateInstance(config) as IRequiredServicesProvider;

                        if (requiredServices?.RequiredSingletonServices?.Count > 0) {
                            singletons.AddRange(requiredServices.RequiredSingletonServices);
                        }
                    }
                }
            }

            void GetRequiredScoped () {
                var scoped = new List<Type>();
                LoadRequiredScoped();

                if (scoped?.Count > 0) {
                    foreach (var type in scoped) {
                        RequiredScopedServices.Add(type);
                    }
                }

                void LoadRequiredScoped () {
                    foreach (var config in requiredServicesConfigurations) {
                        var requiredServices = Activator.CreateInstance(config) as IRequiredServicesProvider;

                        if (requiredServices?.RequiredScopedServices?.Count > 0) {
                            scoped.AddRange(requiredServices.RequiredScopedServices);
                        }
                    }
                }
            }

            void GetRequiredTransient () {
                var transient = new List<Type>();
                LoadRequiredTransient();

                if (transient?.Count > 0) {
                    foreach (var type in transient) {
                        RequiredTransientServices.Add(type);
                    }
                }

                void LoadRequiredTransient () {
                    foreach (var config in requiredServicesConfigurations) {
                        var requiredServices = Activator.CreateInstance(config) as IRequiredServicesProvider;

                        if (requiredServices?.RequiredTransientServices?.Count > 0) {
                            transient.AddRange(requiredServices.RequiredTransientServices);
                        }
                    }
                }
            }
        }
    }
}
