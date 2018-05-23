using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AzureADTest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static PublicClientApplication PublicClientApp = new PublicClientApplication(ClientId);

        private static string ClientId = "eeddd00f-c973-40a1-b345-9f5f6c7d95e0";
    }
}
