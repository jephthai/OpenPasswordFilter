using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;

namespace OPFService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
        public override void Install(IDictionary savedState)
        {
            base.Install(savedState);
            //Add custom code here
        }


        public override void Rollback(IDictionary savedState)
        {
            base.Rollback(savedState);
            //Add custom code here
        }

        public override void Commit(IDictionary savedState)
        {
            RegistryKey lsaKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Lsa", true);
            var registryValue = lsaKey.GetValue("Notification Packages");
            var lsaNotifList = new List<string>(registryValue as string[]);
            lsaNotifList.Add("OpenPasswordFilter");
            lsaKey.SetValue("Notification Packages", lsaNotifList.ToArray());
            lsaKey.Close();
            base.Commit(savedState);
        }


        public override void Uninstall(IDictionary savedState)
        {
            RegistryKey lsaKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Lsa", true);
            var registryValue = lsaKey.GetValue("Notification Packages");
            var lsaNotifList = new List<string>(registryValue as string[]);
            if (lsaNotifList.Contains("OpenPasswordFilter"))
            {
                lsaNotifList.Remove("OpenPasswordFilter");
                lsaKey.SetValue("Notification Packages", lsaNotifList.ToArray());
            }
            lsaKey.Close();
            base.Uninstall(savedState);
        }

    }
}
