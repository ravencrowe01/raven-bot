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
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RavenBot {
    public class Bot {
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

        public Bot () {
            try {
                _config = new ConfigurationBuilder ().AddJsonFile ("config.json", false).Build ();
            }
            catch (FileNotFoundException e) {
                throw new FileNotFoundException ("Configuration file, config.json, not found in current working directory, please include a config file.", e);
            }

            _commandTypes = new List<Type> ();
            _serviceDescriptors = new ServiceCollection ();
        }

        public virtual async Task RunAsync () {
            Setup ();
            await _client.ConnectAsync ().ConfigureAwait (false);
        }

        private void Setup () {
            CreateDiscordClient (_config);
            LoadInteractivity ();
            UseCommandsNext (_client, _config, _serviceDescriptors, _commandTypes);
        }

        protected void CreateDiscordClient (IConfiguration config) {
            _client = new DiscordClient (new DiscordConfiguration () {
                Token = config [TokenKey],
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged
            });
        }

        protected void LoadInteractivity () {
            var interactivitySection = _config.GetSection (InteractivityKey);
            var children = interactivitySection.GetChildren ().ToList ();

            if (children?.Count == 0) {
                return;
            }

            _client.UseInteractivity (new InteractivityConfiguration () {
                Timeout = GetDefaultTimeOut ()
            });

            TimeSpan GetDefaultTimeOut () {
                var timeoutSecton = _config.GetSection (TimeOutKey);

                var timeouts = new Dictionary<string, int> ();

                children.ForEach (c => {
                    timeouts.Add (c.Key, int.Parse (c.Value));
                });

                return new TimeSpan (timeouts [DaysKey], timeouts [HoursKey], timeouts [MinutesKey], timeouts [SecondsKey], timeouts [MilisecondsKey]);
            }
        }

        protected void UseCommandsNext (DiscordClient client, IConfigurationRoot config, IServiceCollection serviceDescriptors, List<Type> commandTypes) {
            var commands = client.UseCommandsNext (new CommandsNextConfiguration () {
                StringPrefixes = new [] { config [PrefixKey] },
                Services = serviceDescriptors.BuildServiceProvider ()
            });

            commandTypes.ForEach (t => commands.RegisterCommands (t));

            _commands = commands;
        }

        public void AddCommandType<T> () where T : BaseCommandModule {
            AddCommandType (typeof (T));
        }

        public void AddCommandType (Type type) {
            if (type.IsAssignableTo (typeof (BaseCommandModule))) {
                _commandTypes.Add (type);
            }
        }

        public void AddRequiredSingletonService<TService, TImplementation> () where TService : class where TImplementation : class => AddRequiredSingletonService (typeof (TService), typeof (TImplementation));

        public void AddRequiredSingletonService (Type service, Type implementation) {
            _serviceDescriptors.TryAddSingleton (service, implementation);
        }

        public void AddRequiredScopedService<TService, TImplementation> () where TService : class where TImplementation : class => AddRequiredScopedService (typeof (TService), typeof (TImplementation));

        public void AddRequiredScopedService (Type service, Type implementation) {
            _serviceDescriptors.TryAddScoped (service, implementation);
        }

        public void AddRequiredTransientService<TService, TImplementation> () where TService : class where TImplementation : class => AddRequiredTransientService (typeof (TService), typeof (TImplementation));

        public void AddRequiredTransientService (Type service, Type implementation) {
            _serviceDescriptors.TryAddTransient (service, implementation);
        }
    }
}
