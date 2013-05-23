using System;
using System.Drawing;
using System.Net;
using System.Windows.Controls;
using System.Windows.Forms;
using DropNet.Exceptions;
using DropNet.Models;
using JetBox.Dropbox;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.UI.Application;
using JetBrains.UI.Controls;
using JetBrains.UI.Options;
using JetBrains.UI.Options.Helpers;
using JetBrains.UI.Options.OptionPages;
using JetBrains.UI.PopupMenu;
using JetBrains.UI.RichText;
using JetBrains.Util;
using LinkLabel = JetBrains.UI.CommonControls.LinkLabel;

namespace JetBox.Options
{
  [OptionsPage(PID, "JetBox", typeof(UnnamedThemedIcons.Dropbox), ParentId = EnvironmentPage.Pid, HelpKeyword = null)]
  public class JetBoxOptionsPage : AStackPanelOptionsPage3
  {
    public const string PID = "JetBox";

    private readonly Client myClient;
    private readonly IContextBoundSettingsStoreLive mySettingsStore;
    private readonly OpensUri myOpensUri;
    private readonly ILogger myLogger;
    
    private readonly RichTextLabel myLoginLabel;
    private readonly FlowLayoutPanel myLoggedPanel;
    private readonly FlowLayoutPanel myNonLoggedPanel;

    public JetBoxOptionsPage(Lifetime lifetime, IUIApplication environment, JetBoxSettingsStorage jetBoxSettings, JetPopupMenus jetPopupMenus, OpensUri opensUri, ILogger logger)
      : base(lifetime, environment, PID)
    {
      mySettingsStore = jetBoxSettings.SettingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide);
      myOpensUri = opensUri;
      myLogger = logger;

      myClient = new Client { UserLogin = mySettingsStore.GetValue(JetBoxSettingsAccessor.Login) };

      // init UI
      myLoggedPanel = new FlowLayoutPanel { Visible = false, AutoSize = true };
      myLoggedPanel.Controls.Add(myLoginLabel = new RichTextLabel(environment));
      myLoggedPanel.Controls.Add(new LinkLabel("Logout", Logout, jetPopupMenus));

      myNonLoggedPanel = new FlowLayoutPanel { Visible = false, AutoSize = true, FlowDirection = FlowDirection.TopDown };
      myNonLoggedPanel.Controls.Add(new LinkLabel("Login", Login, jetPopupMenus));
      myNonLoggedPanel.Controls.Add(new LinkLabel("Get access (click after approving access on the web)", GetInfo, jetPopupMenus));

      Controls.Add(myLoggedPanel);
      Controls.Add(myNonLoggedPanel);

      InitLoginState();
    }

    private void InitLoginState(bool logged)
    {
      if (Control.Control.InvokeRequired)
      {
        Control.Control.Invoke((Action<bool>)InitLoginState, logged);
      }

      if (logged)
      {
        myLoggedPanel.Visible = true;
        myNonLoggedPanel.Visible = false;
      }
      else
      {
        myLoggedPanel.Visible = false;
        myNonLoggedPanel.Visible = true;
      }
    }

    private void InitLoginState()
    {
      if (myClient.UserLogin == null)
      {
        InitLoginState(false);
        return;
      }

      myClient.AccountInfoAsync(info =>
      {
        Control.Control.Invoke((Action)delegate
        {
          myLoginLabel.RichText = new RichText("Logged as ").Append(info.display_name,
            TextStyle.FromForeColor(Color.Blue));
          InitLoginState(true);
        });
      },
      exception =>
      {
        InitLoginState(false);
        LogException(exception);
      });
    }

    private void Login()
    {
      myClient.GetTokenAsync(
        login => myOpensUri.OpenUri(new Uri(myClient.BuildAuthorizeUrl())),
        LogException);
    }

    private void Logout()
    {
      myClient.UserLogin = null;
      mySettingsStore.ResetValue(JetBoxSettingsAccessor.Login);
      InitLoginState(false);
    }

    private void GetInfo()
    {
      if (myClient.UserLogin == null)
      {
        return;
      }
      
      myClient.GetAccessTokenAsync(
        login =>
        {
          mySettingsStore.SetValue(JetBoxSettingsAccessor.Login, login);
          InitLoginState();
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