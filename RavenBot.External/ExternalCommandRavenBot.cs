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
using Microsoft.Extensions.Logging;
using RavenBot.External.Commands;
using RavenBot.External.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RavenBot.External {
    public class ExternalCommandRavenBot : Bot {

        private readonly Func<ILogger<BaseDiscordClient>, ICommandLoader> _commandLoaderBuilder;
        private ICommandLoader _commandLoader;
        private List<Assembly> _commandAssemblies;

        private readonly Func<List<Assembly>, IRequiredServicesProvider> _requiredServicesProviderBuilder;
        private IRequiredServicesProvider _requiredServicesProvider;

        public ExternalCommandRavenBot (Func<ILogger<BaseDiscordClient>, ICommandLoader> commandLoaderBuilder,
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
            Setup ();
            await _client.ConnectAsync ().ConfigureAwait (false);
        }

        private void Setup () {
            CreateDiscordClient (_config);
            LoadInteractivity ();
            LoadCommands ();
            LoadRequiredServices ();
            UseCommandsNext (_client, _config, _serviceDescriptors, _commandTypes);
        }

        private void LoadCommands () {
            _commandLoader ??= _commandLoaderBuilder.Invoke (_client.Logger);

            _commandAssemblies = _commandLoader.LoadCommandAssemblies ();

            var externalCommands = new List<Type> ();

            _commandAssemblies.ForEach (asm => {
                var types = asm.GetTypes ().ToList ();

                types.ForEach (type => {
                    if (type.IsAssignableTo (typeof (BaseCommandModule))) {
                        _commandTypes.Add (type);
                    }
                });
            });
        }

        private void LoadRequiredServices () {
            _requiredServicesProvider ??= _requiredServicesProviderBuilder.Invoke (_commandAssemblies);

            _requiredServicesProvider.RequiredSingletonServices?.ForEach (si => {
                AddRequiredSingletonService (si.Service, si.Implementation);
            });

            _requiredServicesProvider.RequiredScopedServices?.ForEach (si => {
                AddRequiredScopedService (si.Service, si.Implementation);
            });

            _requiredServicesProvider.RequiredTransientServices?.ForEach (si => {
                AddRequiredTransientService (si.Service, si.Implementation);
            });
        }
    }
}
