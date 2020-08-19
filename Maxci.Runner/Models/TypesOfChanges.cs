using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maxci.Runner.Models
{
    /// <summary>
    /// Types of changes for files/folders
    /// </summary>
    internal enum TypesOfChanges
    {
        NewFile = 1,
        UpdateFile = 2,
        DeleteFile = 3,
        NewFolder = 4,
        DeleteFolder = 5
    };
}
