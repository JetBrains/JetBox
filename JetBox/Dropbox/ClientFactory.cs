using System;
using System.Configuration;
using JetBrains.Application;
using JetBrains.Application.Communication;
using JetBrains.Util;

namespace JetBox.Dropbox
{
  [ShellComponent]
  public class ClientFactory
  {
    private readonly ILogger myLogger;
    private readonly WebProxySettingsReader myProxySettingsReader;

    public ClientFactory(ILogger logger, WebProxySettingsReader proxySettingsReader)
    {
      myLogger = logger;
      myProxySettingsReader = proxySettingsReader;
    }

    public Client CreateClient()
    {
      var config = ConfigurationManager.OpenExeConfiguration(GetType().Assembly.Location);
      Assertion.AssertNotNull(config, "There is no config");
      var apiKey = config.AppSettings.Settings["Dropbox.ApiKey"].Value;
      var appSecret = config.AppSettings.Settings["Dropbox.AppSecret"].Value;
      var useSandBox = Convert.ToBoolean(config.AppSettings.Settings["Dropbox.UseSandBox"].Value);
      
      return new Client(myLogger, apiKey, appSecret, myProxySettingsReader.GetWebProxy()) { UseSandbox = useSandBox };
    }
  }
}