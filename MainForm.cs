using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;
using AutoUploadIAToVagtools.Entity;
using Newtonsoft.Json;

namespace AutoUploadIAToVagtools
{
    

    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        public readonly static string _tokenUrl = "https://vagtools.com/api/auth/token";
        public readonly static string _uoloadUrl = "https://vagtools.com/api/system/diagnostic-report/upload";

        private void loginButton_Click(object sender, EventArgs e)
        {
            string username = this.userNameTextBox.Text;
            string password = this.passwordTextBox.Text;

            TokenDto tokenDto = new TokenDto();
            tokenDto.username = username;
            tokenDto.password = password;
            string jsonData = JsonConvert.SerializeObject(tokenDto);
            Dictionary<string, string> headers = new Dictionary<string, string>();
            Token token = doPost<Token>(_tokenUrl, headers, jsonData, "application/json", 30);
            if (token != null)
            {
                this.tokenTextBox.Text = token.token;
            }

        }

        /// <summary>
        /// 发起POST同步请求
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="headers"></param>
        /// <param name="postData"></param>
        /// <param name="contentType"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public static T doPost<T>(string url, Dictionary<string, string> headers = null, string postData = null, string contentType = "application/json", int timeOut = 30)
        {
            return HttpPost(url, headers, postData, contentType, timeOut).ToEntity<T>();
        }

        /// <summary>
        /// 发起同步POST请求, 私有化方法
        /// </summary>
        /// <param name="url"></param>
        /// <param name="headers"></param>
        /// <param name="postData"></param>
        /// <param name="contentType"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        private static string HttpPost(string url, Dictionary<string, string> headers = null, string postData = null, string contentType = "application/json", int timeOut = 30)
        {
            postData = postData ?? "";
            HttpClient httpClient = getDefaultHttpClient(headers, timeOut);
            using (HttpContent httpContent = new StringContent(postData, Encoding.UTF8))
            {
                if (contentType != null)
                {
                    httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                }
                HttpResponseMessage response = httpClient.PostAsync(url, httpContent).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        protected static HttpClient getDefaultHttpClient(Dictionary<string, string> headers, int timeOut = 30)
        {
            HttpClientHandler httpClientHander = new HttpClientHandler();
            httpClientHander.ServerCertificateCustomValidationCallback = delegate { return true; };
            HttpClient client = new HttpClient(httpClientHander);
            client.Timeout = new TimeSpan(0, 0, timeOut);
            if (headers.Count > 0)
            {
                foreach (var header in headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
            return client;
        }

        private void uploadButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("参数私有化: " + this.shareCheckBox.Checked) ;
        }
    }
}
