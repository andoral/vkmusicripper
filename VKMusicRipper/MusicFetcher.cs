using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using HtmlAgilityPack;

namespace VKMusicRipper
{
    public class MusicFetcher
    {

        public static async Task<List<TrackInfo>> GetMusicList()
        {
            if (Authorizer.AuthCookies == null || Authorizer.AuthCookies.Count == 0 || string.IsNullOrEmpty(Authorizer.VkId))
                throw new NullReferenceException("You are not authorized.");

            var Tracks = new List<TrackInfo>();

            int offset = 50; //треков на страницу
            string audioUrl = "https://m.vk.com/audio?offset=";

            using (HttpClientHandler handler = new HttpClientHandler())
            {
                handler.CookieContainer = Authorizer.AuthCookies;
                using (HttpClient client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 7.0; SM-G930V Build/NRD90M) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.125 Mobile Safari/537.36");
                    for (int i = 0; ; i += offset)
                    {
                        string page = await client.GetStringAsync(audioUrl + i);
                        if (!HasTracks(page))
                            break;

                        var audioNodes = GetNodes(page, @"//div[contains(@class, 'audio_item')]");

                        for (int k = 0; k < audioNodes.Count; k++)
                        {
                            try
                            {
                                string maskUrl = audioNodes[k].SelectSingleNode(@".//input[@type='hidden']").GetAttributeValue("value", "");
                                string title = audioNodes[k].SelectSingleNode(@".//div[@class='ai_label']").InnerText;

                                TrackInfo ti = new TrackInfo()
                                {
                                    Title = title,
                                    Uri = UnmaskUrl(maskUrl, Authorizer.VkId)
                                };
                                Tracks.Add(ti);
                            }
                            catch
                            {
                                continue;
                            }
                        };
                    }
                }
            }

            return Tracks;
        }

        private static bool HasTracks(string html)
        {
            var hasTracks = html.Contains("У Вас нет ни одной аудиозаписи");
            return hasTracks;
        }

        private static HtmlNodeCollection GetNodes(string html, string xpath)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var nodes = doc.DocumentNode.SelectNodes(xpath);//(@"//div[contains(@class, ""audio_item"") and contains(@class, ""ai_no_menu""]");
            
            return nodes;
        }

        public static string UnmaskUrl(string maskedUrl, string vkId)
        {
            var js = JsCaller.BuildDownloaderJs(maskedUrl, vkId);

            var unmaskedUrl = JsCaller.EvaluateJs(js);
            return unmaskedUrl;
        }
    }
}
