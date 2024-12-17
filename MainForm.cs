using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Windows.Forms;
using AutoUploadIAToVagtools.Entity;
using IniParser;
using IniParser.Model;
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

        private void MainForm_Load(object sender, EventArgs e)
        {
            var parser = new FileIniDataParser();
            IniData data;
            string configPath = @"C:\Windows\INF\vagtools.ini";
            if (!File.Exists(configPath))
            {
                data = new IniData();
                data["Account"]["username"] = ""; // 设置默认值  
                data["Account"]["password"] = ""; // 设置其他默认值  
                // 将数据写入到 config.ini 文件  
                parser.WriteFile(configPath, data);
            } 
            else
            {
                data = parser.ReadFile(@"C:\Windows\INF\vagtools.ini");
                if (!string.IsNullOrEmpty(data["Account"]["username"]) && !string.IsNullOrEmpty(data["Account"]["password"]))
                {
                    this.userNameTextBox.Text = data["Account"]["username"];
                    this.passwordTextBox.Text = data["Account"]["password"];
                    loginButton_Click(sender, e);
                }
            }
        }

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
                this.loginButton.Enabled = false;
                if (!string.IsNullOrEmpty(this.tokenTextBox.Text))
                {
                    var parser = new FileIniDataParser();
                    IniData data = parser.ReadFile(@"C:\Windows\INF\vagtools.ini");
                    // 写入数据  
                    data["Account"]["username"] = username;
                    data["Account"]["password"] = password;
                    parser.WriteFile(@"C:\Windows\INF\vagtools.ini", data);
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

        private void selectButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "请选择DMS2文件夹的位置 | VagTools.com"; // 对话框描述  
                folderBrowserDialog.ShowNewFolderButton = true; // 允许新建文件夹  
                folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer; // 设置根目录  

                // 显示对话框并检查用户是否选择了文件夹  
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    // 将选择的文件夹路径显示在文本框中  
                    this.dms2PathTextBox.Text = folderBrowserDialog.SelectedPath;
                }
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
