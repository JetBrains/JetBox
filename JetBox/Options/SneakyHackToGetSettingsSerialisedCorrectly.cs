using System;
using System.Linq;
using System.Reflection;
using JetBrains.Application;

namespace JetBox.Options
{
  // Bizarrely, the nuget source settings wouldn't work. Don't really get it, but this is the
  // best explanation yet: http://stackoverflow.com/questions/3612909/why-is-this-typeconverter-not-working
  [ShellComponent]
  public class SneakyHackToGetSettingsSerialisedCorrectly
  {
    public SneakyHackToGetSettingsSerialisedCorrectly()
    {
      AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
    }

    private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
      var domain = (AppDomain)sender;
      return domain.GetAssemblies().FirstOrDefault(asm => asm.FullName == args.Name);
    }
  }
}