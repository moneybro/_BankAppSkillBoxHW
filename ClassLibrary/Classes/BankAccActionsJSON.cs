﻿using ClassLibrary.Classes;
using ClassLibrary.Interfaces;
using ClassLibrary.Methods;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace ClassLibrary.Classes
{
    public class BankAccActionsJSON :
        IBankAccActions,
        IBankAccGetByAccNum,
        ITransactionsActions
    {

        #region создание главного и депозитного счета
        public BankAccMain GetNewMainAcc(long clId)
        {
            BankAccFabric<BankAccMain> newMainAcc = new BankAccFabric<BankAccMain>(new BankAccMain(clId));
            newMainAcc.acc.AccNumber = GetNewAccNumber();
            if (newMainAcc.acc.AccNumber == 0) return null;
            //if (SaveAcc(newMainAcc.acc, GlobalVarsAndActions.MainAccsRepoPath, DateTime.Now))
            //{
            //    return newMainAcc.acc;
            //}
            //else
            //{
            //    return null;
            //}
            return newMainAcc.acc;
        }

        public BankAccDepo GetNewDepoAcc(long clId)
        {
            BankAccFabric<BankAccDepo> newDepoAcc = new BankAccFabric<BankAccDepo>(new BankAccDepo(clId));
            newDepoAcc.acc.AccNumber = GetNewAccNumber();
            if (newDepoAcc.acc.AccNumber == 0) return null;
            DateTime now = DateTime.Now;
            if (SaveAcc(newDepoAcc.acc, GlobalVarsAndActions.DepoAccsRepoPath, now, now))
            {
                return newDepoAcc.acc;
            }
            else
            {
                return null;
            }
            return newDepoAcc.acc;
        }

        internal BankAccForClient GetAccNumberById(long v, object accNum)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region получение нового номера счета для создания нового банковского счета

        /// <summary>
        /// получение нового номера счета для создания нового банковского счета
        /// </summary>
        /// <returns>номер счета</returns>        
        long GetNewAccNumber()
        {
            var accs = GetAllBankAccs();
            long newAccNumber = 0;
                if (accs.Count == 0) 
                {
                    newAccNumber = 1;
                }
                else
                {
                    newAccNumber = accs.Max(a => a.AccNumber) + 1;
                }
            return newAccNumber;
        }
        #endregion

        #region сохранение в бд (в json)
        /// <summary>
        /// метод предназначен для определения типа счета, который закрывается и вызова метода сохранения счета
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="acc"></param>
        /// <returns>истина если сохранение прошло успешно, ложь - сохранение не удалось</returns>
        public bool SaveAcc<T>(T acc, DateTime dateTime) where T : BankAccForClient
        {
            if (acc.GetType() == typeof(BankAccMain))
            {
                SaveAcc(acc, GlobalVarsAndActions.MainAccsRepoPath, default, dateTime);
                return true;
            }
            if (acc.GetType() == typeof(BankAccDepo))
            {
                SaveAcc(acc, GlobalVarsAndActions.DepoAccsRepoPath, default, dateTime);
                return true;
            }
            return false;
        }

        /// <summary>
        /// метод сохранения счета в соответствующее типу хранилище
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="acc"></param>
        /// <param name="bankAccRepo"></param>
        /// <returns></returns>
        public bool SaveAcc<T>(T acc, string bankAccRepo, DateTime createDateTime, DateTime updateDateTime) where T : BankAccForClient
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true, //фрматированный json
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };
            try
            {
                using (FileStream fs = new FileStream(bankAccRepo, FileMode.Append))
                {
                    if (createDateTime == updateDateTime) // если дата создания и изменения равны, то это создание счета, если нет, то изменение
                    {
                        acc.CreateDate = createDateTime;
                    } 
                    acc.UpdateDate = updateDateTime;
                    System.Text.Json.JsonSerializer.SerializeAsync<T>(fs, acc, options);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        //старая
        //public bool SaveAcc<T>(T acc, string bankAccRepo, DateTime dateTime) where T : BankAccForClient
        //        {
        //            var options = new JsonSerializerOptions
        //            {
        //                WriteIndented = true, //фрматированный json
        //                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        //            };
        //            try
        //            {
        //                using (FileStream fs = new FileStream(bankAccRepo, FileMode.Append))
        //                {
        //                    acc.UpdateDate = dateTime;
        //                    System.Text.Json.JsonSerializer.SerializeAsync<T>(fs, acc, options);
        //                }
        //                return true;
        //            }
        //            catch
        //{
        //    return false;
        //}
        //        }
        //}
        #endregion

        #region закрытие счета
        /// <summary>
        /// метод для закрытия счета: поле active переводится в false
        /// </summary>
        /// <param name="accNum"></param>
        /// <returns></returns>
        public bool CloseAcc(long accNum)
        {
            var accs = GetAllBankAccs();
            var accToClose = accs.Find(a => a.AccNumber == accNum);
            if (accToClose == null) { return false; }
            else
            {
                accToClose.Active = false;
                if (SaveAcc(accToClose, DateTime.Now)) return true;
                else return false;
                return true;
            }
        }
        #endregion

        #region получение всех счетов, независимо от типа

        /// <summary>
        /// получение всех счетов, независимо от типа
        /// </summary>
        /// <returns>список счетов</returns>
        List<BankAccForClient> GetAllBankAccs()
        {
            List<BankAccForClient> accs = new List<BankAccForClient>();
            accs.AddRange(getAllMainAccs());
            accs.AddRange(getAllDepoAccs());
            return accs;
        }

        /// <summary>
        /// метод загрузки всех главных счетов
        /// </summary>
        /// <returns>список главных счетов</returns>
        public List<BankAccMain> getAllMainAccs()
        {
            List<BankAccMain> accs = new List<BankAccMain>();
            string bancAccRepo = GlobalVarsAndActions.MainAccsRepoPath;
            using (var sr = new StreamReader(bancAccRepo, new UTF8Encoding()))
            {
                var ser = new Newtonsoft.Json.JsonSerializer();
                var reader = new JsonTextReader(sr);
                while (reader.Read())
                {
                    reader.CloseInput = false;
                    reader.SupportMultipleContent = true;

                    var accObj = ser.Deserialize<BankAccMain>(reader);
                    accs.Add(accObj);
                }
            }
            return accs;
        }

        /// <summary>
        /// метод загрузки всех депозитных счетов
        /// </summary>
        /// <returns>список депозитных счетов</returns>
        public List<BankAccDepo> getAllDepoAccs()
        {
            List<BankAccDepo> accs = new List<BankAccDepo>();
            string bancAccRepo = GlobalVarsAndActions.DepoAccsRepoPath;
            using (var sr = new StreamReader(bancAccRepo, new UTF8Encoding()))
            {
                var ser = new Newtonsoft.Json.JsonSerializer();
                var reader = new JsonTextReader(sr);
                while (reader.Read())
                {
                    reader.CloseInput = false;
                    reader.SupportMultipleContent = true;

                    var accObj = ser.Deserialize<BankAccDepo>(reader);
                    accs.Add(accObj);
                }
            }
            return accs;
        }
        #endregion

        #region получение активных счетов, принадлежащих клиенту
        
        /// <summary>
        /// метод для получения активных счетов, принадлежащих клиенту
        /// </summary>
        /// <param name="clId"></param>
        /// <returns></returns>
        public List<BankAccForClient> GetClientAccs(long clId)
        {
            List<BankAccForClient> bankAccList = new List<BankAccForClient>();

            var clientAccs = GetAllBankAccs()
                .Where(a => a.ClientId == clId)
                .ToList();

            var uniqAccs = clientAccs
                .GroupBy(a => a.AccNumber)
                .ToDictionary(a => a.Key, a => a.ToList());

            foreach (var acc in uniqAccs)
            {
                var lastEl = acc.Value.Last();
                if (lastEl.Active)
                {
                    bankAccList.Add(lastEl);
                }
            }
            return bankAccList;
        }
        #endregion

        #region получение активного счета по его номеру
        public BankAccForClient GetAccByNum(long accNumber)
        {
            var accs = GetAllBankAccs().Where(a => a.AccNumber == accNumber);
            foreach (var acc in accs.Reverse())
            {
                if (acc.Active == true)
                {
                    return acc;
                }
                else continue;
            }
            return null;
        }
        #endregion

        #region получение перечня движений по счету
        public List<BankAccForClient> GetAccTransactions(long accNum)
        {
            var allAccs = GetAllBankAccs();
            var result = allAccs.Where(a => a.AccNumber == accNum).ToList();
            return result;
        }

        #endregion





        #region сохранение транзакции (скорее всего будет отдельный интерфейс чуть позже)

        public bool SaveTransaction(BankAccTransaction tr)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true, //фрматированный json
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };
            try
            {
                string transactionsRepo = DbPaths.getTransactionsPath();
                using (FileStream fs = new FileStream(transactionsRepo, FileMode.Append))
                {
                    System.Text.Json.JsonSerializer.SerializeAsync(fs, tr, options);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region получение всех транзакций
        public List<BankAccTransaction> getAllTransactions()
        {
            List<BankAccTransaction> trs = new List<BankAccTransaction>();
            string trsRepo = DbPaths.getTransactionsPath();
            using (var sr = new StreamReader(trsRepo, new UTF8Encoding()))
            {
                var ser = new Newtonsoft.Json.JsonSerializer();
                var reader = new JsonTextReader(sr);
                while (reader.Read())
                {
                    reader.CloseInput = false;
                    reader.SupportMultipleContent = true;

                    var trsObj = ser.Deserialize<BankAccTransaction>(reader);
                    trs.Add(trsObj);
                }
            }
            return trs;
        }
        #endregion

        public List<BankAccTransactionFull> GetBankAccTransactionsFull(long accNum)
        {
            List<BankAccTransactionFull> trsf = new List<BankAccTransactionFull>();
            var accs = GetAccTransactions(accNum);
            var trs = getAllTransactions();
            foreach (var acc in accs)
            {
                foreach (var tr in trs)
                {
                    if ((tr.AccNumberSource == accNum || tr.AccNumberTarget == accNum) && acc.UpdateDate == tr.Date)
                    {
                        trsf.Add(new BankAccTransactionFull() { Acc = acc, Tr = tr });
                    }
                }
            }
            return trsf;
        }
    }
}
