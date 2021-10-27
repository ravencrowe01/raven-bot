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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RavenBot {
    public class RavenBot {
        public const string TokenKey = "token";
        public const string PrefixKey = "prefix";
        public const string InteractivityKey = "interactivity";
        public const string TimeOutKey = "timeout";
        public const string DaysKey = "days";
        public const string HoursKey = "hours";
        public const string MinutesKey = "minutes";
        public const string SecondsKey = "seconds";
        public const string MilisecondsKey = "miliseconds";

        protected IConfigurationRoot _config;

        protected List<Type> _commandTypes;

        protected IServiceCollection _serviceDescriptors;

        protected DiscordClient _client;
        protected CommandsNextExtension _commands;


        public RavenBot () {
            try {
                _config = new ConfigurationBuilder().AddJsonFile("config.json", false).Build();
            }
            catch (FileNotFoundException e) {
                throw new FileNotFoundException("Configuration file, config.json, not found in current working directory, please include a config file.", e);
            }

            _commandTypes = new List<Type>();
            _serviceDescriptors = new ServiceCollection();
        }

        public virtual async Task RunAsync () {
            Setup();
            await _client.ConnectAsync().ConfigureAwait(false);
        }

        protected void Setup () {
            _client = CreateDiscordClient(_config);
            LoadInteractivity();
            _commands = UseCommandsNext(_client, _config, _serviceDescriptors, _commandTypes);

            static DiscordClient CreateDiscordClient (IConfiguration config) => 
                new(new DiscordConfiguration() {
                Token = config[TokenKey],
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged
            });

            static CommandsNextExtension UseCommandsNext (DiscordClient client, IConfigurationRoot config, IServiceCollection serviceDescriptors, List<Type> commandTypes) {
                var commands = client.UseCommandsNext(new CommandsNextConfiguration() {
                    StringPrefixes = new[] { config[PrefixKey] },
                    Services = serviceDescriptors.BuildServiceProvider()
                });

                commandTypes.ForEach(t => commands.RegisterCommands(t));

                return commands;
            }

            void LoadInteractivity () {
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
        }

        public void AddCommandType<T> () where T : BaseCommandModule {
            AddCommandType(typeof(T));
        }

        public void AddCommandType (Type type) {
            if (type.IsAssignableTo(typeof(BaseCommandModule))) {
                _commandTypes.Add(type);
            }
        }

        public void AddRequiredSingletonService<TService, TImplementation> () where TService : class where TImplementation : class => AddRequiredSingletonService(typeof(TService), typeof(TImplementation));

        public void AddRequiredSingletonService (Type service, Type implementation) {
            _serviceDescriptors = _serviceDescriptors.AddSingleton(service, implementation);
        }

        public void AddRequiredScopedService<TService, TImplementation> () where TService : class where TImplementation : class => AddRequiredScopedService(typeof(TService), typeof(TImplementation));

        public void AddRequiredScopedService (Type service, Type implementation) {
            _serviceDescriptors = _serviceDescriptors.AddScoped(service, implementation);
        }

        public void AddRequiredTransientService<TService, TImplementation> () where TService : class where TImplementation : class => AddRequiredTransientService(typeof(TService), typeof(TImplementation));

        public void AddRequiredTransientService (Type service, Type implementation) {
            _serviceDescriptors = _serviceDescriptors.AddTransient(service, implementation);
        }
    }
}
