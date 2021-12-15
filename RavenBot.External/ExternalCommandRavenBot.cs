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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RavenBot.External.Commands;
using RavenBot.External.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RavenBot.External {
    public class ExternalCommandRavenBot : Bot {
        private string _commandDirectory = Directory.GetCurrentDirectory () + @"\commands";

        private ICommandLoader _commandLoader;

        private IRequiredServicesProvider _requiredServicesProvider;

        public ExternalCommandRavenBot (ICommandLoader commandLoader, IRequiredServicesProvider requiredServicesProvider) {
            _commandLoader = commandLoader;
            _requiredServicesProvider = requiredServicesProvider;
        }

        private ExternalCommandRavenBot () { }

        public override async Task RunAsync () {
            var setup = Setup ();

            if (setup) {
                await _client.ConnectAsync ().ConfigureAwait (false);
            }
        }

        private bool Setup () {
            CreateDiscordClient (_config);
            LoadInteractivity ();

            var created = TryCreateCommandDirectory (_client.Logger);

            if (created) {
                _client.Logger.LogError ("No commands were present. A commands directory was created. Please add commands to the directory then run again.");
                return false;
            }

            _commandTypes.AddRange( _commandLoader.LoadCommandTypes (_commandDirectory).ToList ());

            _serviceDescriptors.Add (_requiredServicesProvider.LoadRequiredServices (_commandDirectory));

            UseCommandsNext (_client, _config, _serviceDescriptors, _commandTypes);

            return true;
        }

        private bool TryCreateCommandDirectory (ILogger<BaseDiscordClient> logger) {
            if (!Directory.Exists (_commandDirectory)) {
                try {
                    Directory.CreateDirectory (_commandDirectory);
                    return true;
                }
                catch (IOException) {
                    logger.LogWarning ("A file named 'commands' exists in working directory.");
                }
                catch (UnauthorizedAccessException) {
                    logger.LogWarning ("Tried to create commands directory, access was denied.");
                }
            }

            return false;
        }
    }
}
