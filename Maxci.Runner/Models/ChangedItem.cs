using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maxci.Runner.Models
{
    /// <summary>
    /// File or folder that has been changed
    /// </summary>
    internal class ChangedItem
    {
        /// <summary>
        /// Type of changes for current item
        /// </summary>
        public TypesOfChanges TypeOfChanges { get; private set; }

        /// <summary>
        /// Relative path to file or folder
        /// </summary>
        public string Path { get; private set; }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="path">Path to file/folder</param>
        /// <param name="typeOfChanges">Type of changes</param>
        public ChangedItem(string path, TypesOfChanges typeOfChanges)
        {
            Path = path;
            TypeOfChanges = typeOfChanges;
        }
    }
}
