using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net.Http;
using System.Net;

namespace VKMusicRipper
{
    public class Authorizer
    {
        public static CookieContainer AuthCookies;
        public static string VkId;

        public static async Task<bool> InitAuthorizedCookies(string login, string password)
        {
            try
            {
                using (var handler = new HttpClientHandler())
                {
                    handler.CookieContainer = new CookieContainer();
                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 7.0; SM-G930V Build/NRD90M) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.125 Mobile Safari/537.36");

                        string html = await client.GetStringAsync("https://m.vk.com/login");
                        string actionValue = FindActionValue(html);
                        Uri authUri = new Uri(actionValue);
                        var body = new Dictionary<string, string> { { "act", "login" }, { "email", login }, { "pass", password } };
                        var response = await client.PostAsync(authUri, new FormUrlEncodedContent(body));
                        var page = await response.Content.ReadAsStringAsync();

                        if (!page.Contains("act=logout")) // если нет кнопки выхода, значит авторизация не удалась
                            return false;

                        VkId = handler.CookieContainer.GetCookies(new Uri("https://login.vk.com"))["l"].Value;
                        AuthCookies = handler.CookieContainer;
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private static string FindActionValue(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            string action = doc.DocumentNode.SelectNodes(@"//form")[0].GetAttributeValue("action", "");
            return action;
        }
    }
}
