using System;
using System.Net;
using DropNet.Exceptions;
using DropNet.Models;
using JetBox.Dropbox;
using JetBrains.DataFlow;
using JetBrains.UI.Application;
using JetBrains.UI.CommonControls;
using JetBrains.UI.Options;
using JetBrains.UI.Options.Helpers;
using JetBrains.UI.Options.OptionPages;
using JetBrains.UI.PopupMenu;
using JetBrains.Util;

namespace JetBox.Options
{
  [OptionsPage(PID, "JetBox", typeof(UnnamedThemedIcons.Dropbox), ParentId = EnvironmentPage.Pid, HelpKeyword = null)]
  public class JetBoxOptionsPage : AStackPanelOptionsPage3
  {
    public const string PID = "JetBox";

    private readonly Client myClient;
    private readonly OpensUri myOpensUri;
    private readonly ILogger myLogger;

    public JetBoxOptionsPage(Lifetime lifetime, IUIApplication environment, JetPopupMenus jetPopupMenus, OpensUri opensUri, ILogger logger)
      : base(lifetime, environment, PID)
    {
      myClient = new Client();
      myOpensUri = opensUri;
      myLogger = logger;
      Controls.Add(new LinkLabel("Login", Login, jetPopupMenus));
      Controls.Add(new LinkLabel("Get info", GetInfo, jetPopupMenus));
    }

    private void Login()
    {
      myClient.GetTokenAsync(
        login => myOpensUri.OpenUri(new Uri(myClient.BuildAuthorizeUrl())),
        LogException);
    }

    private void GetInfo()
    {
      myClient.GetAccessTokenAsync(
        login =>
        {
          var info = myClient.AccountInfo();
          MessageBox.ShowInfo("User: " + info.display_name);
        },
        LogException);
    }

    private void LogException(DropboxException exception)
    {
      switch (exception.StatusCode)
      {
        case HttpStatusCode.Unauthorized:
          break;

        default:
          myLogger.LogForeignException(exception);
          break;
      }
    }
  }
}