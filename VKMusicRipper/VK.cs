using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using HtmlAgilityPack;
using CefSharp.OffScreen;
using CefSharp;

namespace VKMusicRipper
{
    class VK
    {
        static CookieContainer authCookies;
        static List<TrackInfo> Tracks;

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

                    authCookies = handler.CookieContainer;
                    return true;
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public static async Task<string[]> GetMusicList(CefSharp.Cookie cookie = null)
        {
            if (authCookies == null || authCookies.Count == 0)
                throw new NullReferenceException("You are not authorized.");

            Tracks = new List<TrackInfo>();

            int offset = 50; //треков на страницу
            string audioUrl = "https://m.vk.com/audio?offset=";

            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = authCookies;
            using (HttpClient client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 7.0; SM-G930V Build/NRD90M) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.125 Mobile Safari/537.36");
                for (int i = 0; ; i += 50)
                {
                    string page = await client.GetStringAsync(audioUrl + i);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(page);
                    var audioNodes = doc.DocumentNode.SelectNodes(@"//div[contains(@class, 'audio_item')]");//(@"//div[contains(@class, ""audio_item"") and contains(@class, ""ai_no_menu""]");
                    
                    var settings = new CefSharp.BrowserSettings()
                    {
                        DefaultEncoding = "UTF-8",
                        RemoteFonts = CefSharp.CefState.Disabled,
                        ImageLoading = CefSharp.CefState.Disabled
                    };
                    var settings2 = new CefSettings()
                    {
                        UserAgent = ""
                    };
                    Cef.Initialize(settings2);
                    var browser = new ChromiumWebBrowser(browserSettings: settings);
                    //browser.LoadHtml(page, "http:///csharp.org");
                    var cm = browser.RequestContext.GetDefaultCookieManager(null);
                    cm.SetCookie("https://vk.com/audio",cookie);
                    browser.Load("https://vk.com/audio");
                    
                    for (int k = 0; k < audioNodes.Count; k++)
                    {
                        string url = audioNodes[k].SelectSingleNode(@".//input[@type='hidden']").GetAttributeValue("value", "") + '\n';
                        string title = audioNodes[k].SelectSingleNode(@".//div[@class='ai_label']").InnerText;
                        JavascriptResponse dsa = await browser.GetMainFrame().EvaluateScriptAsync("alert(\"x\")");
                        TrackInfo ti = new TrackInfo()
                        {
                            Title = title,
                            T = url,
                            Uri = dsa.Result as string
                        };
                        Tracks.Add(ti);
                    };
                }
            }
        }
    }

    class TrackInfo
    {
        public string Title { get; set; }
        /// <summary>
        /// ссылка вида https://...audio_api_unavailable.mp3...
        /// </summary>
        public string T { get; set; }
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
