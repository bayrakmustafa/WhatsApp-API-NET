using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace WhatsAppApi.Parser
{
    public class PhoneNumber
    {
        public string Country;
        public string CC;
        public string Number;

        public string FullNumber
        {
            get
            {
                return this.CC + this.Number;
            }
        }

        public string ISO3166;
        public string ISO639;
        protected string _Mcc;
        protected string _Mnc;

        public string MCC
        {
            get
            {
                //return this._Mcc.PadLeft(3, '0');
                return this._Mcc;
            }
        }

        public string MNC
        {
            get
            {
                //return this._Mnc.PadLeft(3, '0');
                return this._Mnc;

            }
        }

        public PhoneNumber(string number)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("WhatsAppApi.Parser.Countries.csv"))
            {
                using (StreamReader reader = new System.IO.StreamReader(stream))
                {
                    string csv = reader.ReadToEnd();
                    string[] lines = csv.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        string[] values = line.Trim(new char[] { '\r' }).Split(new char[] { ',' });
                        //Try to Match
                        if (number.StartsWith(values[1]))
                        {
                            //Matched
                            this.Country = values[0].Trim(new char[] { '"' });

                            //Hook: Fix CC for North America
                            if (values[1].StartsWith("1"))
                            {
                                values[1] = "1";
                            }
                            this.CC = values[1];
                            this.Number = number.Substring(this.CC.Length);
                            this.ISO3166 = values[3].Trim(new char[] { '"' });
                            this.ISO639 = values[4].Trim(new char[] { '"' });
                            this._Mcc = values[2].Trim(new char[] { '"' });
                            this._Mnc = values[5].Trim(new char[] { '"' });
                            if (this._Mcc.Contains('|'))
                            {
                                //Take First One
                                string[] parts = this._Mcc.Split(new char[] { '|' });
                                this._Mcc = parts[0];
                            }

                            return;
                        }
                    }
                    //Could Not Match!
                    throw new Exception(String.Format("Could Not Dissect Phone Number {0}", number));
                }
            }
        }

        public static string DetectMnc(string lc, String carrierName)
        {
            String mnc = "000";
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("WhatsAppApi.Parser.NetworkInfo.csv"))
            {
                using (StreamReader reader = new System.IO.StreamReader(stream))
                {
                    string csv = reader.ReadToEnd();
                    string[] lines = csv.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        string[] values = line.Trim(new char[] { '\r' }).Split(new char[] { ',' });
                        //Try to Match
                        if (lc.Equals(values[4], StringComparison.InvariantCultureIgnoreCase) && carrierName.Equals(values[7], StringComparison.InvariantCultureIgnoreCase))
                        {
                            mnc = values[2];
                            break;
                        }
                    }
                }
            }

            return mnc;
        }
    }
}