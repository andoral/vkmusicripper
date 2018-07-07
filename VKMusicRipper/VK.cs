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
        private static CookieContainer authCookies;
        private static string VkId;

        public static async Task<bool> Authorize(string login, string password)
        {
            try
            {
                HttpClientHandler handler = new HttpClientHandler();
                handler.CookieContainer = new CookieContainer();
                using (HttpClient client = new HttpClient(handler))
                {
                    //client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
                    //client.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.6,en;q=0.4");
                    //client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                    //client.DefaultRequestHeaders.Add("Accept", "*/*");
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 7.0; SM-G930V Build/NRD90M) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.125 Mobile Safari/537.36");

                    string data = await client.GetStringAsync("https://m.vk.com/login");
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(data);
                    string action = doc.DocumentNode.SelectNodes(@"//form")[0].GetAttributeValue("action", "");
                    Uri authUri = new Uri(action);
                    var body = new Dictionary<string, string> { { "act", "login" }, { "email", login }, { "pass", password } };
                    var response = await client.PostAsync(authUri, new FormUrlEncodedContent(body));
                    var page = await response.Content.ReadAsStringAsync();
                   
                    if (!page.Contains("act=logout")) // если нет кнопки выхода, значит авторизация не удалась
                        return false;

                    VkId = handler.CookieContainer.GetCookies(new Uri("https://login.vk.com"))["l"].Value;
                    authCookies = handler.CookieContainer;
                    return true;
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public static async Task<List<TrackInfo>> GetMusicList()
        {
            if (authCookies == null || authCookies.Count == 0 || string.IsNullOrEmpty(VkId))
                throw new NullReferenceException("You are not authorized.");

            var Tracks = new List<TrackInfo>();

            int offset = 50; //треков на страницу
            string audioUrl = "https://m.vk.com/audio?offset=";

            using (HttpClientHandler handler = new HttpClientHandler())
            {
                handler.CookieContainer = authCookies;
                using (HttpClient client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 7.0; SM-G930V Build/NRD90M) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.125 Mobile Safari/537.36");
                    for (int i = 0; ; i += offset)
                    {
                        string page = await client.GetStringAsync(audioUrl + i);
                        if (page.Contains("У Вас нет ни одной аудиозаписи"))
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
                                    Uri = UnmaskUrl(maskUrl, VkId)
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
