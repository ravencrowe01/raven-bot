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
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RavenBot.External.Commands {
    public class CommandLoader : ICommandLoader {
        private readonly ILogger<BaseDiscordClient> _logger;

        public CommandLoader(ILogger<BaseDiscordClient> logger) {
            _logger = logger;
        }

        public List<Assembly> LoadCommandAssemblies () {
            var assemblies = new List<Assembly>();
            var commandDir = Directory.GetCurrentDirectory() + @"\commands";

            if (!Directory.Exists(commandDir)) {
                try {
                    Directory.CreateDirectory(commandDir);
                }
                catch (IOException) {
                    _logger.LogWarning("A file named 'commands' exists in working directory.");
                }
                catch (UnauthorizedAccessException) {
                    _logger.LogWarning("Tried to create commands directory, access was denied.");
                }
                return assemblies;
            }

            Directory.GetFiles(commandDir, "*.dll").ToList().ForEach(ProcessAssembly);

            return assemblies;

            void ProcessAssembly (string p) {
                var asm = Assembly.LoadFrom(p);

                if (DoesAssemblyHaveCommands(asm)) {
                    assemblies.Add(asm);
                }

                static bool DoesAssemblyHaveCommands (Assembly asm) =>
                    asm.GetTypes().Any(t => t.IsAssignableTo(typeof(IRequiredServices))
                                            || t.IsAssignableTo(typeof(BaseCommandModule)));
            }
        }

    }
}
