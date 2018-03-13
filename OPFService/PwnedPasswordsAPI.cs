using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Net;

namespace OPFService
{
  class PwnedPasswordsAPI
    // Troy Hunt has kindly made available a k-anonymity type query API for checking
    // password hashes against his massive collection of breach corpuses (corpi?).
    // This adds an implementation of that to OpenPasswordFilter.
    //
    // We compute the sha1 hash of the potential password and send the first five
    // characters over to Troy, who replies with a list of all the suffixes that 
    // share that same hash prefix. We can thus check for previously pwned passwords
    // without disclosing sha1 hashes in full, which would just be silly.
  {
    private const string urlBase = "https://api.pwnedpasswords.com/range/";
    private const string userAgent = "OpenPasswordFilter";

    public void writeLog(string message, System.Diagnostics.EventLogEntryType level)
    {
      using (EventLog eventLog = new EventLog("Application"))
      {
        eventLog.Source = "Application";
        eventLog.WriteEntry(message, level, 101, 1);
      }
    }

    internal bool checkHashPrefix(string password)
    {
      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
      List<string> pwnedHashes = new List<string>();
      byte[] bytes = Encoding.UTF8.GetBytes(password);
      var sha1 = SHA1.Create();
      byte[] hashBytes = sha1.ComputeHash(bytes);
      var sb = new StringBuilder();
      foreach (byte b in hashBytes)
      {
        var hex = b.ToString("x2");
        sb.Append(hex);
      }
      string hexHash = sb.ToString();
      string hashPrefix = hexHash.Substring(0, 5);
      string url = urlBase + hashPrefix;

      HttpClient client = new HttpClient();
      client.BaseAddress = new Uri(urlBase);
      client.DefaultRequestHeaders.Add("User-Agent", userAgent);
      try
      {
        HttpResponseMessage response = client.GetAsync(hashPrefix).Result;
        if (response.IsSuccessStatusCode)
        {
          var dataObjects = response.Content.ReadAsStringAsync().Result;
          foreach (var d in dataObjects.Split('\n'))
          {
            string suffix = d.Split(':').First().ToLower();
            pwnedHashes.Add(hashPrefix + suffix);
          }
          if (pwnedHashes.Contains(hexHash))
          {
            writeLog("This password is found in breach corpuses at haveibeenpwned.com", EventLogEntryType.Information);
            return true;
          }
        }
        else
        {
          writeLog("PwnedPasswordsAPI returned an error", EventLogEntryType.Error);
          return false;
        }
      }
      catch (Exception e)
      {
        writeLog(e.Message, EventLogEntryType.Error);
      }
      return false;
      
    }
  }
}
