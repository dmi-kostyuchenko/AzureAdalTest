using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AzureADTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string _aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static string _tenant = ConfigurationManager.AppSettings["ida:Tenant"];

        private static string _webClientId = ConfigurationManager.AppSettings["ida:WebClientId"];
        private static string _webAppKey = ConfigurationManager.AppSettings["ida:WebAppKey"];

        private static string _nativeClientId = ConfigurationManager.AppSettings["ida:NativeClientId"];
        private static string _nativeRedirectUri = ConfigurationManager.AppSettings["ida:NativeRedirectUri"];

        private static string _webApiResourceId = ConfigurationManager.AppSettings["webapi:ResourceId"];
        private static string _webApiBaseAddress = ConfigurationManager.AppSettings["webapi:BaseAddress"];

        private static string _authority = String.Format(CultureInfo.InvariantCulture, _aadInstance, _tenant);

        public MainWindow()
        {
            InitializeComponent();

            HttpClient = new HttpClient();
            AuthContext = new AuthenticationContext(_authority);
            ClientCredential = new ClientCredential(_webClientId, _webAppKey);
        }

        private Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationResult AdalAuthenticationResult { get; set; }

        private HttpClient HttpClient { get; }
        private AuthenticationContext AuthContext { get; }
        private Microsoft.IdentityModel.Clients.ActiveDirectory.ClientCredential ClientCredential { get; }

        private String AccessToken
        {
            get
            {
                return AdalAuthenticationResult?.AccessToken;
            }
        }

        private async void SingInInteractively_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //AdalAuthenticationResult = await AuthContext.AcquireTokenAsync(_webApiResourceId, _clientId, new Uri("http://localhost"), new PlatformParameters(PromptBehavior.Always));
                AdalAuthenticationResult = await AuthContext.AcquireTokenAsync(_webApiResourceId, _nativeClientId, new Uri(_nativeRedirectUri), new PlatformParameters(PromptBehavior.Always));

                TokenInfoText.Text += $"Access Token: {AdalAuthenticationResult.AccessToken}{Environment.NewLine}";
                TokenInfoText.Text += $"Token Expires: {AdalAuthenticationResult.ExpiresOn.ToLocalTime()}{Environment.NewLine}";
            }
            catch (Exception ex)
            {
                TokenInfoText.Text += String.Format("An error occurred while acquiring a token\nTime: {0}\nError: {1}\n",
                    DateTime.Now.ToString(),
                    ex.ToString());
            }
        }

        private async void SingInSilently_Click(object sender, RoutedEventArgs e)
        {
            var retryCount = 0;
            var retry = false;

            do
            {
                retry = false;
                try
                {
                    // ADAL includes an in memory cache, so this call will only send a message to the server if the cached token is expired.
                    AdalAuthenticationResult = await AuthContext.AcquireTokenAsync(_webApiResourceId, ClientCredential);

                    TokenInfoText.Text += $"Token Expires: {AdalAuthenticationResult.ExpiresOn.ToLocalTime()}{Environment.NewLine}";
                    TokenInfoText.Text += $"Access Token: {AdalAuthenticationResult.AccessToken}{Environment.NewLine}";
                }
                catch (AdalException ex)
                {
                    retry = ex.ErrorCode == "temporarily_unavailable";

                    TokenInfoText.Text += String.Format("An error occurred while acquiring a token\nTime: {0}\nError: {1}\nRetry: {2}\n",
                        DateTime.Now.ToString(),
                        ex.ToString(),
                        retry.ToString());
                }

            } while (retry && (retryCount < 3));
        }

        private async void TestAPI_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(AccessToken))
            {
                ResultText.Text += "Canceling attempt to contact API.\n";
                return;
            }

            ResultText.Text += $"Testing API using '{AccessToken.Substring(AccessToken.Length - 50, 50)}' access token{Environment.NewLine}";

            await DeleteAllData();

            for (int i = 0; i < 4; ++i)
                await PostData();

            await GetData();
        }

        private async Task PostData()
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

            var value = new Random().Next().ToString();
            HttpContent content = new StringContent(value, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await HttpClient.PostAsync($"{_webApiBaseAddress}/api/values", content);

            if (response.IsSuccessStatusCode == true)
            {
                ResultText.Text += $"Successfully posted:  {value}{Environment.NewLine}";
            }
            else
            {
                ResultText.Text += $"Failed to post a data\nError:  {response.ReasonPhrase}{Environment.NewLine}";
            }
        }

        private async Task GetData()
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

            ResultText.Text += $"Retrieving data list at {DateTime.Now.ToString()}";
            HttpResponseMessage response = await HttpClient.GetAsync($"{_webApiBaseAddress}/api/values");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var serializer = new JavaScriptSerializer();
                var data = serializer.Deserialize<String[]>(content);

                ResultText.Text += String.Join(Environment.NewLine, data);
                ResultText.Text += Environment.NewLine;
                ResultText.Text += $"Total item count:  {data.Length}{Environment.NewLine}";
            }
            else
            {
                ResultText.Text += $"Failed to retrieve data\nError:  {response.ReasonPhrase}{Environment.NewLine}";
            }
        }

        private async Task DeleteAllData()
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

            ResultText.Text += $"Deleting data list at {DateTime.Now.ToString()}{Environment.NewLine}";
            HttpResponseMessage response = await HttpClient.DeleteAsync($"{_webApiBaseAddress}/api/values");

            if (response.IsSuccessStatusCode)
            {
                ResultText.Text += "The data has been deleted\n";
            }
            else
            {
                ResultText.Text += $"Failed to retrieve data\nError:  {response.ReasonPhrase}{Environment.NewLine}";
            }
        }
    }
}
