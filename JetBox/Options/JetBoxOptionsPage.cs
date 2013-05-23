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

    public JetBoxOptionsPage(Lifetime lifetime, IUIApplication environment, JetPopupMenus jetPopupMenus)
      : base(lifetime, environment, PID)
    {
      Controls.Add(new LinkLabel("Login", Login, jetPopupMenus));
    }

    private void Login()
    {
      MessageBox.ShowInfo("Soon");
    }
  }
}