using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VKMusicRipper
{
    public class TrackInfo
    {
        public string Title { get; set; }
        /// <summary>
        /// ссылка на mp3
        /// </summary>
        public string Uri { get; set; }
        /// <summary>
        /// размер mp3 в байтах
        /// </summary>
        public int Size { get; set; }
        public int SizeInKBytes { get { return Size / 1024; } }
    }
}
