using System;
using System.Collections.Generic;

namespace Twinlib
{
    internal class Repository
    {
        internal class Platform
        {
            public string Name { get; set; }
            public IEnumerable<Library> Libraries { get; set; }
        }
        internal class Library
        {
            public string Name { get; set; }
            public Version LibraryVersion { get; set; }
            public string Distributor { get; set; }
        }

        public Dictionary<string, IEnumerable<Library>> Platforms = new Dictionary<string, IEnumerable<Library>>();
    }
}
