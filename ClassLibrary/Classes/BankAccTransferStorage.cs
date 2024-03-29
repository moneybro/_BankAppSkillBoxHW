﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary.Interfaces;

namespace ClassLibrary.Classes
{
    public class BankAccTransferStorage<T> : IStorageTransferMoney<T> where T : BankAccBase
    {
        List<T> db;
        public BankAccTransferStorage()
        {
            db = new List<T>();
        }
        public T addAcc { 
            set
            {
                if (db.Count < 2)
                {
                    if (db.Count == 0) Console.WriteLine($"transfer from acc {value.Amount}");
                    else Console.WriteLine($"transfer to acc {value.Amount}");
                    db.Add(value);
                }
                else
                {
                    Console.WriteLine("two accs added yet! can not more!");
                }
            }
        }
        public bool TransferMoney(decimal summ, DateTime dateTime, IBankAccActions accActions)
        {
            if (db.Count == 2)
            {
                var accSource = accActions.GetAccByNum(db[0].AccNumber);
                var accTarget = accActions.GetAccByNum(db[1].AccNumber);

                accSource.Amount -= summ;
                accTarget.Amount += summ;

                db[0].Amount = accSource.Amount;
                db[1].Amount = accTarget.Amount;

                // если информация об счетах не сохранилась, то возвращаем баланс 
                var successSource = accActions.SaveAcc(accSource, dateTime);
                var successTarget = accActions.SaveAcc(accTarget, dateTime);

                if (!successSource || !successTarget)
                {
                    accSource.Amount += summ;
                    accTarget.Amount -= summ;
                    //db[0].Amount += summ;
                    //db[1].Amount -= summ;
                    db[0].Amount = accSource.Amount;
                    db[1].Amount = accTarget.Amount;


                    accActions.SaveAcc(accSource, dateTime);
                    accActions.SaveAcc(accTarget, dateTime);
                    return false;
                }
                else return true;
            }
            else
            {
                return false;
            }
        }
    }
}
