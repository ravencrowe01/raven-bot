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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RavenBot {
    public class RavenBot {
        public const string TokenKey = "token";
        public const string PrefixKey = "prefix";

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
            _commands = UseCommandsNext(_client, _config, _serviceDescriptors, _commandTypes);

            DiscordClient CreateDiscordClient (IConfiguration config) {
                return new DiscordClient(new DiscordConfiguration() {
                    Token = config[TokenKey],
                    TokenType = TokenType.Bot,
                    Intents = DiscordIntents.AllUnprivileged
                });
            }

            static CommandsNextExtension UseCommandsNext (DiscordClient client, IConfigurationRoot config, IServiceCollection serviceDescriptors, List<Type> commandTypes) {
                var commands = client.UseCommandsNext(new CommandsNextConfiguration() {
                    StringPrefixes = new[] { config[PrefixKey] },
                    Services = serviceDescriptors.BuildServiceProvider()
                });

                commandTypes.ForEach(t => commands.RegisterCommands(t));

                return commands;
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

        public void AddRequiredSingletonService<T> () where T : class {
            _serviceDescriptors.AddSingleton<T>();
        }

        public void AddRequiredSingletonService (Type type) {
            _serviceDescriptors.AddSingleton(type);
        }

        public void AddRequiredScopedService<T> () where T : class {
            _serviceDescriptors.AddScoped<T>();
        }

        public void AddRequiredScopedService (Type type) {
            _serviceDescriptors.AddScoped(type);
        }

        public void AddRequiredTransientService<T> () where T : class {
            _serviceDescriptors.AddTransient<T>();
        }

        public void AddRequiredTransientService (Type type) {
            _serviceDescriptors.AddTransient(type);
        }
    }
}
