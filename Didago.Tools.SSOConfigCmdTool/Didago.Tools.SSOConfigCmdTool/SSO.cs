using Microsoft.EnterpriseSingleSignOn.Interop;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Transactions;

namespace Didago.Tools.SSOConfigCmdTool
{
    /// Author  : Jean-Paul Smit - Didago IT Consultancy
    /// Date    : July 18, 2012
    /// The code for this part of the tool is taken from the MMC snap-in for SSO
    /// The MMC snap-in can be found here: http://www.microsoft.com/en-us/download/details.aspx?id=14524

    internal class SSO
    {
        public string strCompany = "";
        public string strSecrectServer = "";
        public string strSSOAdminGroup = "";
        public string strAffiliateAppMgrGroup = "";
        public string strSsoDBServer = "";
        public string strSsoDB = "";
        private static string CONFIG_NAME = "ConfigProperties";

        public SSO(string dbServer, string dbName, string company)
        {
            strSsoDBServer = dbServer;
            strSsoDB = dbName;
            strCompany = company;
        }

        public string[] GetApplications()
        {
            string[] array = new string[10];
            if (this.strSecrectServer == null || this.strSecrectServer == "")
            {
                this.GetSecretServerName();
            }
            string[] result;
            try
            {
                string commandText = "Select ai_app_name from SSOX_ApplicationInfo where ai_contact_info not in ('someone@microsoft.com', 'someone@companyname.com')";
                SqlConnection sqlConnection = new SqlConnection();
                sqlConnection.ConnectionString = string.Concat(new string[]
				{
					"Data Source=",
					this.strSsoDBServer,
					"; Initial Catalog=",
					this.strSsoDB,
					"; Integrated Security=SSPI"
				});
                sqlConnection.Open();
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(new SqlCommand
                {
                    Connection = sqlConnection,
                    CommandText = commandText
                });
                DataSet dataSet = new DataSet();
                sqlDataAdapter.Fill(dataSet, "Result");
                int count = dataSet.Tables[0].Rows.Count;
                array = new string[count];
                for (int i = 0; i < count; i++)
                {
                    array[i] = dataSet.Tables[0].Rows[i][0].ToString();
                }
                sqlConnection.Close();
                result = array;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("SSOConfigCmdTool - GetApplications", ex.Message);
                string[] array2 = new string[10];
                result = array2;
            }
            return result;
        }
        public void GetSecretServerName()
        {
            try
            {
                ISSOAdmin2 iSSOAdmin = (ISSOAdmin2)new SSOAdmin();
                int num;
                int num2;
                int num3;
                int num4;
                int num5;
                int num6;
                int num7;
                iSSOAdmin.GetGlobalInfo(out num, out num2, out num3, out num4, out num5, out num6, out num7, out this.strSecrectServer, out this.strSSOAdminGroup, out this.strAffiliateAppMgrGroup);
                this.strSsoDBServer = (Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\Microsoft\\ENTSSO\\SQL", "Server", "") as string);
                this.strSsoDB = (Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\Microsoft\\ENTSSO\\SQL", "Database", "") as string);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("SSOConfigCmdTool - GetSecretServerName", ex.Message);
            }
        }
        private void CreateApplicationFields(string appName, ISSOAdmin admin, string[] arrKeys)
        {
            try
            {
                int flags = 536870912;
                int num = arrKeys.Length;
                admin.CreateFieldInfo(appName, string.Format("biztalkadmin@{0}.com", strCompany), flags);
                for (int i = 0; i < num; i++)
                {
                    admin.CreateFieldInfo(appName, arrKeys[i].ToString(), flags);
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("SSOConfigCmdTool - CreateApplicationFields", ex.Message);
            }
        }
        public void CreateApplicationFieldsValues(string name, string[] arrKeys, string[] arrValues)
        {
            try
            {
                this.DeleteApplication(name);
                this.CreateApplication(name, arrKeys);
                this.SaveApplicationData(name, arrKeys, arrValues);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("SSOConfigCmdTool - CreateApplicationFieldsValues", ex.Message);
            }
        }
        public void CreateApplication(string name, string[] arrKeys)
        {
            try
            {
                using (TransactionScope transactionScope = new TransactionScope())
                {
                    int numFields = arrKeys.Length;
                    int flags = 1310720;
                    ISSOAdmin iSSOAdmin = (ISSOAdmin)new SSOAdmin();
                    this.Enlist(iSSOAdmin, Transaction.Current);
                    iSSOAdmin.CreateApplication(name, name + " Configuration Data", string.Format("biztalkadmin@{0}.com", strCompany), this.strAffiliateAppMgrGroup, this.strSSOAdminGroup, flags, numFields);
                    this.CreateApplicationFields(name, iSSOAdmin, arrKeys);
                    this.EnableApplication(name, iSSOAdmin);
                    transactionScope.Complete();
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("SSOConfigCmdTool - CreateApplication", ex.Message);
            }
        }
        public void SaveApplicationData(string appName, string[] arrKeys, string[] arrValues)
        {
            try
            {
                int num = arrKeys.Length;
                SSOPropertyBag sSOPropertyBag = new SSOPropertyBag();
                for (int i = 0; i < num; i++)
                {
                    sSOPropertyBag.SetValue<string>(arrKeys[i], arrValues[i]);
                }
                ISSOConfigStore iSSOConfigStore = (ISSOConfigStore)new SSOConfigStore();
                iSSOConfigStore.SetConfigInfo(appName, SSO.CONFIG_NAME, sSOPropertyBag);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("SSOConfigCmdTool - SaveApplicationData", ex.Message);
            }
        }
        public void DeleteApplication(string appName)
        {
            try
            {
                using (TransactionScope transactionScope = new TransactionScope())
                {
                    ISSOAdmin iSSOAdmin = (ISSOAdmin)new SSOAdmin();
                    this.Enlist(iSSOAdmin, Transaction.Current);
                    iSSOAdmin.DeleteApplication(appName);
                    transactionScope.Complete();
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("SSOConfigCmdTool - DeleteApplication", ex.Message);
            }
        }
        private void Enlist(object obj, Transaction tx)
        {
            try
            {
                IPropertyBag propertyBag = (IPropertyBag)obj;
                object dtcTransaction = TransactionInterop.GetDtcTransaction(tx);
                ISSOAdmin2 iSSOAdmin = (ISSOAdmin2)new SSOAdmin();
                int num;
                int num2;
                int num3;
                int num4;
                int num5;
                int num6;
                int num7;
                iSSOAdmin.GetGlobalInfo(out num, out num2, out num3, out num4, out num5, out num6, out num7, out this.strSecrectServer, out this.strSSOAdminGroup, out this.strAffiliateAppMgrGroup);
                object obj2 = this.strSecrectServer;
                propertyBag.Write("CurrentSSOServer", ref obj2);
                propertyBag.Write("Transaction", ref dtcTransaction);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("SSOConfigCmdTool - Enlist", ex.Message);
            }
        }
        private void EnableApplication(string appName, ISSOAdmin admin)
        {
            try
            {
                int num = 2;
                admin.UpdateApplication(appName, null, null, null, null, num, num);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("SSOConfigCmdTool - EnableApplication", ex.Message);
            }
        }
        public string[] GetKeys(string appName)
        {
            string[] result;
            try
            {
                SSOPropertyBag sSOPropertyBag = new SSOPropertyBag();
                ISSOConfigStore iSSOConfigStore = (ISSOConfigStore)new SSOConfigStore();
                iSSOConfigStore.GetConfigInfo(appName, SSO.CONFIG_NAME, 4, sSOPropertyBag);
                string[] array = new string[sSOPropertyBag._dictionary.Count];
                int num = 0;
                foreach (KeyValuePair<string, object> current in sSOPropertyBag._dictionary)
                {
                    array[num] = current.Key.ToString();
                    num++;
                }
                result = array;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("SSOConfigCmdTool - GetKeys", ex.Message);
                result = new string[]
				{
					"ERROR: " + ex.Message
				};
            }
            return result;
        }
        public string[] GetValues(string appName)
        {
            string[] result;
            try
            {
                SSOPropertyBag sSOPropertyBag = new SSOPropertyBag();
                ISSOConfigStore iSSOConfigStore = (ISSOConfigStore)new SSOConfigStore();
                iSSOConfigStore.GetConfigInfo(appName, SSO.CONFIG_NAME, 4, sSOPropertyBag);
                string[] array = new string[sSOPropertyBag._dictionary.Count];
                int num = 0;
                foreach (KeyValuePair<string, object> current in sSOPropertyBag._dictionary)
                {
                    array[num] = current.Value.ToString();
                    num++;
                }
                result = array;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("SSOConfigCmdTool - GetValues", ex.Message);
                result = new string[]
				{
					""
				};
            }
            return result;
        }
        public string Decrypt(string toDecrypt, string key)
        {
            string @string;
            try
            {
                byte[] array = Convert.FromBase64String(toDecrypt);
                MD5CryptoServiceProvider mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
                byte[] key2 = mD5CryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(key));
                ICryptoTransform cryptoTransform = new TripleDESCryptoServiceProvider
                {
                    Key = key2,
                    Mode = CipherMode.ECB,
                    Padding = PaddingMode.PKCS7
                }.CreateDecryptor();
                byte[] bytes = cryptoTransform.TransformFinalBlock(array, 0, array.Length);
                @string = Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("SSOConfigCmdTool - Decrypt", ex.Message);
                throw ex;
            }
            return @string;
        }

        public string Encrypt(string toEncrypt, string key)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(toEncrypt);
            MD5CryptoServiceProvider mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
            byte[] key2 = mD5CryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(key));
            ICryptoTransform cryptoTransform = new TripleDESCryptoServiceProvider
            {
                Key = key2,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            }.CreateEncryptor();
            byte[] array = cryptoTransform.TransformFinalBlock(bytes, 0, bytes.Length);
            return Convert.ToBase64String(array, 0, array.Length);
        }
    }
}
