using System;
using System.Collections;
using System.Text;
using System.IO;

namespace ws
{
    class KeyStore
    {
        public Hashtable data = new Hashtable();
        public string path = null;

        public void Load()
        {
            if (!File.Exists(path))
            {
                return;
            }
            lock (this)
            {
                string crc = string.Empty;
                Crc nCrc = new Crc(true);
                using (Stream s = Utils.OpenRead(path))
                {
                    using (StreamReader sr = new StreamReader(s))
                    {
                        for (string line = sr.ReadLine(); line != null; line = sr.ReadLine())
                        {
                            line = line.Trim(' ', '\t', '\r', '\n');
                            if (string.IsNullOrEmpty(line)) continue;
                            if (line.StartsWith(Crc.Prefix))
                            {
                                crc = line.Substring(Crc.Prefix.Length);
                                continue;
                            }
                            if (line.StartsWith("#")) continue;
                            int idx = line.IndexOf('=');
                            if (idx <= 0) continue;
                            string key = line.Substring(0, idx).Trim();
                            string val = line.Substring(idx + 1).Trim();
                            data[key] = val;
                            nCrc.Update(key);
                            nCrc.Update(val);
                        }
                    }
                }
                //if (!string.IsNullOrEmpty(crc))
                //{
                    string newCrc = nCrc.GetValue();
                    if (!crc.ToLower().Equals(newCrc))
                    {
                        this.data.Clear();
                        RawLog.Default.Log("store " + Str.Def.Get(Str.FailedCrc), true);
                    }
                //}
            }
        }

        public void Save() 
        {
            lock (this)
            {
                using (Stream s = Utils.OpenWrite(path))
                {
                    using (StreamWriter sw = new StreamWriter(s))
                    {
                        Crc crc = new Crc(true);
                        foreach (string k in data.Keys)
                        {
                            string v = (string)data[k];
                            if (v == null) v = string.Empty;
                            sw.WriteLine(k + "=" + v);
                            crc.Update(k);
                            crc.Update(v);
                        }
                        sw.WriteLine(Crc.Prefix + crc.GetValue());
                    }
                }
            }
        }

        public string GetString(string id, string defVal, bool reload) 
        {
            lock (this)
            {
                if (reload || (data.Count <= 0))
                {
                    Load();
                }
                string v = (string)data[id];
                return ((v == null) ? defVal : v);
            }
        }

        public void SetString(string id, string val, bool save) 
        {
            lock (this)
            {
                if (id == null) return;
                if (val == null) val = string.Empty;
                data[id] = val;
                if (save)
                {
                    Save();
                }
            }
        }

        public int GetInt(string id, int defVal, bool reload)
        {
            string v = GetString(id, defVal.ToString(System.Globalization.CultureInfo.InvariantCulture), reload);
            if (string.IsNullOrEmpty(v)) return defVal;
            return Convert.ToInt32(v, System.Globalization.CultureInfo.InvariantCulture);
        }

        public void SetInt(string id, int val, bool save)
        {
            SetString(id, val.ToString(System.Globalization.CultureInfo.InvariantCulture), save);
        }

        public long GetLong(string id, long defVal, bool reload) 
        {
            string v = GetString(id, defVal.ToString(System.Globalization.CultureInfo.InvariantCulture), reload);
            if (string.IsNullOrEmpty(v)) return defVal;
            return Convert.ToInt64(v, System.Globalization.CultureInfo.InvariantCulture);
        }

        public void SetLong(string id, long val, bool save)
        {
            SetString(id, val.ToString(System.Globalization.CultureInfo.InvariantCulture), save);
        }

        public double GetDouble(string id, double defVal, bool reload)
        {
            string v = GetString(id, defVal.ToString(System.Globalization.CultureInfo.InvariantCulture), reload);
            if (string.IsNullOrEmpty(v)) return defVal;
            return Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture);
        }

        public void SetDouble(string id, double val, bool save)
        {
            SetString(id, val.ToString(System.Globalization.CultureInfo.InvariantCulture), save);
        }

        public bool GetBool(string id, bool defVal, bool reload)
        {
            int i = GetInt(id, defVal ? 1 : 0, reload);
            return (i == 1);
        }

        public void SetBool(string id, bool val, bool save)
        {
            SetInt(id, val ? 1 : 0, save);
        }

    }//EOC
}
