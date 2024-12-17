using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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

        public static string _tokenUrl = "https://vagtools.com/api/auth/token";
        public static string _uploadUrl = "https://vagtools.com/api/dataset/files/upload";

        private void loginButton_Click(object sender, EventArgs e)
        {
            string username = this.userNameTextBox.Text;
            string password = this.passwordTextBox.Text;

            TokenDto tokenDto = new TokenDto();
            tokenDto.username = username;
            tokenDto.password = password;
            string jsonData = JsonConvert.SerializeObject(tokenDto);
            Dictionary<string, string> headers = new Dictionary<string, string>();
            Token token = doPost<Token>(_tokenUrl, headers, jsonData, "application/json", 60);
            if (token != null)
            {
                this.tokenTextBox.Text = token.token;
                if (!string.IsNullOrEmpty(this.tokenTextBox.Text))
                {
                    this.uploadButton.Enabled = true;
                }
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

        private async void uploadButton_Click(object sender, EventArgs e)
        {
            HttpResponseMessage response = null;
            string responseBody = string.Empty;
            string token = this.tokenTextBox.Text;
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", "Bearer " + token);
            using (var formData = new MultipartFormDataContent())
            {
                var files = Directory.GetFiles(this.dms2PathTextBox.Text);
                foreach (var file in files)
                {
                    var fileStream = new FileStream(file, FileMode.Open);
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "files",
                        FileName = Path.GetFileName(file)
                    };
                    formData.Add(fileContent);
                }
                formData.Add(new StringContent(this.shareCheckBox.Checked.ToString()), "share");
                HttpClient client = getDefaultHttpClient(headers, 60);
                response = await client.PostAsync(_uploadUrl, formData);
                responseBody = await response.Content.ReadAsStringAsync();
            }
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("上传成功!");
            }
            else
            {
                MessageBox.Show(responseBody);
            }
        }
    }

    public static class JsonExtends
    {
        public static T ToEntity<T>(this string val)
        {
            return JsonConvert.DeserializeObject<T>(val);
        }

        public static string ToJson<T>(this T entity, Formatting formatting = Formatting.None)
        {
            return JsonConvert.SerializeObject(entity, formatting);
        }
    }
}
