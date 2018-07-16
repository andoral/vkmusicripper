using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace VKMusicRipper
{
    class VK
    {
        public async Task<bool> Authorize(string login, string password)
        {
            var authorized = await Authorizer.InitAuthorizedCookies(login, password);
            return authorized;
        }

        public async Task<List<TrackInfo>> GetMusicList()
        {
            var list = await MusicFetcher.GetMusicList();
            return list;
        }
        

        
    }
}
