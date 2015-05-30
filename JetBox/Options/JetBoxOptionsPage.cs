using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
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
    private readonly SequentialLifetimes myLifetimes;
    
    private readonly RichTextLabel myLoginLabel;
    private readonly FlowLayoutPanel myLoggedPanel;
    private readonly FlowLayoutPanel myNonLoggedPanel;

    public JetBoxOptionsPage(Lifetime lifetime, IUIApplication environment, ClientFactory clientFactory, JetBoxSettingsStorage jetBoxSettings, JetPopupMenus jetPopupMenus)
      : base(lifetime, environment, PID)
    {
      mySettingsStore = jetBoxSettings.SettingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide);
      myLifetimes = new SequentialLifetimes(lifetime);

      myClient = clientFactory.CreateClient();
      myClient.UserLogin = mySettingsStore.GetValue(JetBoxSettingsAccessor.Login);

      // init UI
      myLoggedPanel = new FlowLayoutPanel { Visible = false, AutoSize = true };
      myLoggedPanel.Controls.Add(myLoginLabel = new RichTextLabel(environment));
      myLoggedPanel.Controls.Add(new LinkLabel("Logout", Logout, jetPopupMenus));

      myNonLoggedPanel = new FlowLayoutPanel { Visible = false, AutoSize = true, FlowDirection = FlowDirection.TopDown };
      myNonLoggedPanel.Controls.Add(new LinkLabel("Login", Login, jetPopupMenus)
      {
        Image = Environment.Theming.Icons[UnnamedThemedIcons.Dropbox.Id].CurrentGdipBitmapScreenDpi,
        ImageAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(20, 0, 0, 0)
      });

      Controls.Add(myLoggedPanel);
      Controls.Add(myNonLoggedPanel);

      InitLoginState();
    }

    private void InitLoginState(bool logged)
    {
      if (Control.Control.InvokeRequired)
      {
        Control.Control.Invoke((Action<bool>)InitLoginState, logged);
        return;
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
        myClient.LogException(exception);
      });
    }

    private void Login()
    {
      myLifetimes.Next(lifetime =>
      {
        myClient.UserLogin = null;
        myClient.GetTokenAsync(
          login =>
          {
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            int port;
            try
            {
              tcpListener.Start();
              port = ((IPEndPoint) tcpListener.LocalEndpoint).Port;
            }
            finally
            {
              tcpListener.Stop();
            }
            var callbackUri = string.Format("http://{0}:{1}/", "localhost", port);
            var server = new HttpListener();
            server.Prefixes.Add(callbackUri);
            server.Start();

            lifetime.AddDispose(server);
            server.BeginGetContext(ar =>
            {
              myClient.Logger.CatchAsOuterDataError(() =>
              {
                if (lifetime.IsTerminated) return;
                var context = server.EndGetContext(ar);
                // Write a response.
                using (var writer = new StreamWriter(context.Response.OutputStream))
                {
                  string response = @"<html>
    <head>
      <title>JetBox - OAuth Authentication</title>
    </head>
    <body>
      <h1>Authorization for JetBox</h1>
      <p>The application has received your response. You can close this window now.</p>
      <script type='text/javascript'>
        window.setTimeout(function() { window.open('', '_self', ''); window.close(); }, 100);
        if (window.opener) { window.opener.checkToken(); }
       </script>
    </body>
  </html>";
                  writer.WriteLine(response);
                  writer.Flush();
                }
                context.Response.OutputStream.Flush();
                context.Response.OutputStream.Close();
                context.Response.Close();
                GetInfo();
              });
            }, null);
            Environment.OpensUri.OpenUri(new Uri(myClient.BuildAuthorizeUrl(callbackUri)));
          },
          myClient.LogException);
      });
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
        myClient.LogException);
    }
  }
}