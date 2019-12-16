using NUnit.Framework;
using System;
using System.Web;
using System.Collections.Specialized;

namespace LeanCloud.Play {
	public class ChoreTest {
		[SetUp]
		public void SetUp() {
			Common.Logger.LogDelegate += Utils.Log;
		}

		[TearDown]
		public void TearDown() {
			Common.Logger.LogDelegate -= Utils.Log;
		}

		[Test]
		public void ParseUrl() {
            Uri uri = new Uri("wss://cn-e1-cell3.leancloud.cn:5769/?appId=hahahaha&group=leanengine&instance=XSDSDS");
            string url = $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}";
            Common.Logger.Debug(url);
            NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(uri.Query);
            foreach (string key in nameValueCollection.Keys) {
                Common.Logger.Debug($"{key} : {nameValueCollection.Get(key)}");
            }
        }
	}
}

