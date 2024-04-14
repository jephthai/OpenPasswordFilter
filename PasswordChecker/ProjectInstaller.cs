using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;

namespace PasswordCheckerRay
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void serviceProcessInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {

        }

        /*
        public override void Install(IDictionary mySavedState)
        {
            base.Install(mySavedState);
            // Code maybe written for installation of an application.
            Console.WriteLine("The Install method of 'MyInstallerSample' has been called");
        }
        */
    }
}
