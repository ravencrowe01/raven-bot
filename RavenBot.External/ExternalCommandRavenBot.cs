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

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using RavenBot.External.Commands;
using RavenBot.External.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RavenBot.External {
    public class ExternalCommandRavenBot : RavenBot {
        public const string InteractivityKey = "interactivity";
        public const string TimeOutKey = "timeout";
        public const string DaysKey = "days";
        public const string HoursKey = "hours";
        public const string MinutesKey = "minutes";
        public const string SecondsKey = "seconds";
        public const string MilisecondsKey = "miliseconds";

        private readonly Func<ILogger<BaseDiscordClient>, ICommandLoader> _commandLoaderBuilder;
        private ICommandLoader _commandLoader;
        private List<Assembly> _commandAssemblies;

        private readonly Func<List<Assembly>, IRequiredServicesProvider> _requiredServicesProviderBuilder;
        private IRequiredServicesProvider _requiredServicesProvider;

        public ExternalCommandRavenBot(Func<ILogger<BaseDiscordClient>, ICommandLoader> commandLoaderBuilder,
                                        Func<List<Assembly>, IRequiredServicesProvider> requiredServicesProviderBuilder) {
            _commandLoaderBuilder = commandLoaderBuilder;
            _requiredServicesProviderBuilder = requiredServicesProviderBuilder;
        }

        public ExternalCommandRavenBot (ICommandLoader commandLoader, IRequiredServicesProvider requiredServicesProvider) {
            _commandLoader = commandLoader;
            _requiredServicesProvider = requiredServicesProvider;
        }

        private ExternalCommandRavenBot () { }

        public override async Task RunAsync () {
            Setup();
            LoadInteractivity();
            LoadCommands();
            LoadRequiredServices();
            await base.RunAsync();

            void LoadInteractivity() {
                var interactivitySection = _config.GetSection(InteractivityKey);
                var children = interactivitySection.GetChildren().ToList();

                if (children?.Count == 0) {
                    return;
                }

                _client.UseInteractivity(new InteractivityConfiguration() { 
                    Timeout = GetDefaultTimeOut()
                });

                TimeSpan GetDefaultTimeOut () {
                    var timeoutSecton = _config.GetSection(TimeOutKey);
                    var days = 0;
                    var hours = 0;
                    var minutes = 0;
                    var seconds = 30;
                    var miliseconds = 0;

                    children.ForEach(c => {
                        switch (c.Key) {
                            case DaysKey:
                                days = int.Parse(c.Value);
                                break;
                            case HoursKey:
                                hours = int.Parse(c.Value);
                                break;
                            case MinutesKey:
                                minutes = int.Parse(c.Value);
                                break;
                            case SecondsKey:
                                seconds = int.Parse(c.Value);
                                break;
                            case MilisecondsKey:
                                miliseconds = int.Parse(c.Value);
                                break;
                        }
                    });

                    return new TimeSpan(days, hours, minutes, seconds, miliseconds);
                }
            }

            void LoadCommands () {
                _commandLoader ??= _commandLoaderBuilder.Invoke(_client.Logger);

                _commandAssemblies = _commandLoader.LoadCommandAssemblies();

                _commandAssemblies.ForEach(asm => {
                    var types = asm.GetTypes().ToList();

                    types.ForEach(type => {
                        if (type.IsAssignableTo(typeof(BaseCommandModule))) {
                            _commandTypes.Add(type);
                        }
                    });
                });
            }

            void LoadRequiredServices () {
                _requiredServicesProvider ??= _requiredServicesProviderBuilder.Invoke(_commandAssemblies);

                _requiredServicesProvider.RequiredSingletonServices?.ForEach(AddRequiredSingletonService);

                _requiredServicesProvider.RequiredScopedServices?.ForEach(AddRequiredScopedService);

                _requiredServicesProvider.RequiredTransientServices?.ForEach(AddRequiredTransientService);
            }
        }
    }
}
