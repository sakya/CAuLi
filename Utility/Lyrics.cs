using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Utility;

public static class Lyrics
{
    #region Classes
    [XmlRoot("GetLyricResult", Namespace = "http://api.chartlyrics.com/")]
    public class ChartLyrics
    {
        public ChartLyrics()
        {

        }

        [XmlElement("TrackId")]
        public int TrackId { get; set; }

        public string LyricChecksum { get; set; }
        public int LyricId { get; set; }
        public string LyricSong { get; set; }
        public string LyricArtist { get; set; }
        public string LyricUrl { get; set; }
        public string LyricCovertArtUrl { get; set; }
        public int LyricRank { get; set; }
        public string LyricCorrectUrl { get; set; }
        public string Lyric { get; set; }
    } // ChartLyrics

    [XmlRoot("result")]
    public class LoloLyrics
    {
        [XmlElement("status")]
        public string Status { get; set; }

        [XmlElement("response")]
        public string Response { get; set; }
    } // LoloLyrics
    #endregion

    #region private operations
    private static async Task<string> GetContent(string url)
    {
        string res = string.Empty;
        try {
            using (HttpClient httpClient = new HttpClient()) {
                using (HttpResponseMessage response = await httpClient.GetAsync(new Uri(url))) {
                    res = await response.Content.ReadAsStringAsync();
                }
            }
        } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine(string.Format("GetContent error: {0}", ex.Message));
        }
        return res;
    } // GetContent

    private static T Deserialize<T>(string data)
    {
        try {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            T result;
            using (StringReader sr = new StringReader(data)) {
                result = (T)serializer.Deserialize(sr);
            }
            return result;
        } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine(string.Format("Error deserializing object: {0}", ex.Message));
            if (ex.InnerException != null)
                System.Diagnostics.Debug.WriteLine(ex.InnerException.Message);
        }
        return default(T);
    } // Deserialize
    #endregion

    #region public operations
    public static async Task<string> GetLyrics(string artist, string title)
    {
        string response = string.Empty;
        string url = string.Empty;

        // ChartLyrics
        url = string.Format("http://api.chartlyrics.com/apiv1.asmx/SearchLyricDirect?artist={0}&song={1}",
            Uri.EscapeDataString(artist), Uri.EscapeDataString(title));
        response = await GetContent(url);
        if (!string.IsNullOrEmpty(response)) {
            ChartLyrics cl = Deserialize<ChartLyrics>(response);
            if (cl != null && !string.IsNullOrEmpty(cl.Lyric))
                return WebUtility.HtmlDecode(cl.Lyric);
        }

        // LoloLyrics
        url = string.Format("http://api.lololyrics.com/0.5/getLyric?artist={0}&track={1}",
            Uri.EscapeDataString(artist), Uri.EscapeDataString(title));
        response = await GetContent(url);
        if (!string.IsNullOrEmpty(response)) {
            LoloLyrics ll = Deserialize<LoloLyrics>(response);
            if (ll != null && ll.Status == "OK" && !string.IsNullOrEmpty(ll.Response))
                return WebUtility.HtmlDecode(ll.Response);
        }

        return string.Empty;
    } // GetLyrics
    #endregion
}