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
using System.IO;
using System.Linq;
using System.Reflection;

namespace RavenBot.External.Commands {
    public class CommandLoader : ICommandLoader {
        public IEnumerable<Type> LoadCommandTypes (string workingDirectory) {
            if (!Directory.Exists (workingDirectory)) {
                throw new IOException ($"The directory, {workingDirectory}, does not exist.");
            }

            var assemblyPaths = Directory.GetFiles (workingDirectory, "commands_*.dll").ToList ();

            var assemblies = new List<Assembly> ();

            foreach (var assemblyPath in assemblyPaths) {
                var asm = Assembly.Load (assemblyPath);
                assemblies.Add (asm);
            }

            return GetCommandTypes (assemblies);
        }

        public IEnumerable<Type> LoadCommandTypes (IEnumerable<Assembly> assemblies) {
            var commands = new List<Type> ();

            var found = GetCommandTypes (assemblies);

            if (found?.Count () > 0) {
                commands.AddRange (found);
            }

            return commands;
        }

        private static IEnumerable<Type> GetCommandTypes (IEnumerable<Assembly> assemblies) => (from asm in assemblies
                                                                                                from type in asm.GetTypes ()
                                                                                                where type.IsAssignableTo (typeof (IRequiredServices))
                                                                                                select type).ToList ();
    }
}
