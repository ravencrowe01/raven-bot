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

using RavenBot.Required;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RavenBot.External.Services {
    public class RequiredServicesProvider : IRequiredServicesProvider {
        public List<RequiredService> RequiredSingletonServices { get; } = new ();
        public List<RequiredService> RequiredScopedServices { get; } = new ();
        public List<RequiredService> RequiredTransientServices { get; } = new ();

        public RequiredServicesProvider (IReadOnlyCollection<Assembly> commandAssemblies) {
            GetRequiredServices (commandAssemblies);
        }

        private void GetRequiredServices (IReadOnlyCollection<Assembly> commandAssemblies) {
            var requiredServicesConfigurations = GetServiceConfigurations ();

            GetRequiredSingletons ();
            GetRequiredScoped ();
            GetRequiredTransient ();

            IList<Type> GetServiceConfigurations () {
                return (from asm in commandAssemblies
                        from type in asm.GetTypes ()
                        where type.IsAssignableTo (typeof (IRequiredServices))
                        select type).ToList ();
            }

            void GetRequiredSingletons () {
                var singletons = LoadRequiredSingletons ();

                if (singletons?.Count > 0) {
                    foreach (var service in singletons) {
                        RequiredSingletonServices.Add (service);
                    }
                }

                List<RequiredService> LoadRequiredSingletons () {
                    var services = new List<RequiredService> ();

                    foreach (var config in requiredServicesConfigurations) {
                        var requiredServices = Activator.CreateInstance (config) as IRequiredServices;

                        if (requiredServices?.RequiredSingletons?.Count > 0) {
                            services.AddRange (requiredServices.RequiredSingletons);
                        }
                    }

                    return services;
                }
            }

            void GetRequiredScoped () {
                var scoped = LoadRequiredScoped ();

                if (scoped?.Count > 0) {
                    foreach (var service in scoped) {
                        RequiredScopedServices.Add (service);
                    }
                }

                List<RequiredService> LoadRequiredScoped () {
                    var services = new List<RequiredService> ();

                    foreach (var config in requiredServicesConfigurations) {
                        var requiredServices = Activator.CreateInstance (config) as IRequiredServices;

                        if (requiredServices?.RequiredScoped?.Count > 0) {
                            services.AddRange (requiredServices.RequiredScoped);
                        }
                    }

                    return services;
                }
            }

            void GetRequiredTransient () {
                var transient = LoadRequiredTransient ();

                if (transient?.Count > 0) {
                    foreach (var service in transient) {
                        RequiredTransientServices.Add (service);
                    }
                }

                List<RequiredService> LoadRequiredTransient () {
                    var services = new List<RequiredService> ();

                    foreach (var config in requiredServicesConfigurations) {
                        var requiredServices = Activator.CreateInstance (config) as IRequiredServices;

                        if (requiredServices?.RequiredTransient?.Count > 0) {
                            services.AddRange (requiredServices.RequiredTransient);
                        }
                    }

                    return services;
                }
            }
        }
    }
}
