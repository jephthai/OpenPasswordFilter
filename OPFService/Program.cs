// This file is part of OpenPasswordFilter.
// 
// OpenPasswordFilter is free software; you can redistribute it and / or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// OpenPasswordFilter is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with OpenPasswordFilter; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111 - 1307  USA
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.ServiceProcess;
using System.IO;

namespace OPFService {
    class OPFService : ServiceBase {
        Thread worker;

        public OPFService() {
        }

        static void Main(string[] args) {
            ServiceBase.Run(new OPFService());
        }

        protected override void OnStart(string[] args) {
            base.OnStart(args);
            // OPFDictionary d = new OPFDictionary(AppDomain.CurrentDomain.BaseDirectory + "\\opfdict.txt");
            OPFDictionary d = new OPFDictionary("c:\\opf\\opfdict.txt");
            NetworkService svc = new NetworkService(d);
            worker = new Thread(() => svc.main());
            worker.Start();
        }

        protected override void OnShutdown() {
            base.OnShutdown();
            worker.Abort();
        }
    }
}
