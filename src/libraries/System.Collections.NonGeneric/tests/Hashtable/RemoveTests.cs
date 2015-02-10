// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System.Text;
using System;
using System.Collections;

namespace System.Collections.HashtableTests
{
    public class RemoveTests
    {
        [Fact]
        public void TestRemove()
        {
            Hashtable hash = null;
            int ii;

            HashConfuse hshcnf1;
            string strValue;
            ArrayList alst;

            Boolean fRetValue;
            int iCount;
            Random rnd1;
            int iElement;

            #region "TestData"
            string[] strSuperHeroes =
            {
                "Captain Marvel" ,      //0
                "Batgirl" ,         //1
                "Nightwing" ,           //2
                "Green Lantern" ,       //3
                "Robin" ,               //4
                "Superman" ,            //5
                "Black Canary" ,        //6
                "Spiderman" ,           //7
                "Iron Man" ,            //8
                "Wonder Girl" ,     //9
                "Batman" ,              //10
                "Flash" ,               //11
                "Green Arrow" ,     //12
                "Atom" ,                //13
                "Steel" ,               //14
                "Powerman" ,            //15
            };

            string[] strSecretIdentities =
            {
                "Batson, Billy" ,       //0
                "Gordan, Barbara" , //1
                "Grayson, Dick" ,       //2
                "Jordan, Hal" ,     //3
                "Drake, Tim" ,          //4
                "Kent, Clark" ,     //5
                "Lance, Dinah" ,        //6
                "Parker, Peter" ,       //7
                "Stark, Tony" ,     //8
                "Troy, Donna" ,     //9
                "Wayne, Bruce" ,        //10
                "West, Wally" ,     //11
                "Queen, Oliver" ,       //12
                "Palmer, Ray" ,     //13
                "Irons, John Henry" ,   //14
                "Cage, Luke" ,          //15
            };
            #endregion

            // Allocate the hash table.
            hash = new Hashtable();
            Assert.NotNull(hash);

            // Construct the hash table by adding items to the table.
            for (ii = 0; ii < strSuperHeroes.Length; ++ii)
            {
                hash.Add(strSuperHeroes[ii], strSecretIdentities[ii]);
            }

            // Validate additions to Hashtable.
            Assert.Equal(strSuperHeroes.Length, hash.Count);

            //
            // [] Remove: Attempt to remove a bogus key entry from table.
            //
            hash.Remove("THIS IS A BOGUS KEY");
            Assert.Equal(strSuperHeroes.Length, hash.Count);

            //
            // [] Remove: Attempt to remove a null key entry from table.
            //
            Assert.Throws<ArgumentNullException>(() =>
                         {
                             hash.Remove(null);
                         }
            );

            //
            // [] Remove: Add key/value pair to Hashtable and remove items.
            //

            // Remove items from Hashtable.
            for (ii = 0; ii < strSuperHeroes.Length; ++ii)
            {
                hash.Remove(strSuperHeroes[ii]);
                Assert.Equal(strSuperHeroes.Length - ii - 1, hash.Count);
            }

            //[]We want to add and delete items (with the same hashcode) to the hashtable in such a way that the hashtable
            //does not expand but have to tread through collision bit set positions to insert the new elements. We do this
            //by creating a default hashtable of size 11 (with the default load factor of 0.72), this should mean that
            //the hashtable does not expand as long as we have at most 7 elements at any given time?

            hash = new Hashtable();
            alst = new ArrayList();
            for (int i = 0; i < 7; i++)
            {
                strValue = "Test_" + i;
                hshcnf1 = new HashConfuse(strValue);
                alst.Add(hshcnf1);
                hash.Add(hshcnf1, strValue);
            }

            //we will delete and add 3 new ones here and then compare
            fRetValue = true;
            iCount = 7;
            rnd1 = new Random(-55);
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    if (!((string)hash[alst[j]]).Equals(((HashConfuse)alst[j]).Word))
                    {
                        fRetValue = false;
                    }
                }

                //we delete 3 elements from the hashtable
                for (int j = 0; j < 3; j++)
                {
                    iElement = rnd1.Next(6);
                    hash.Remove(alst[iElement]);
                    alst.RemoveAt(iElement);

                    strValue = "Test_" + iCount++;
                    hshcnf1 = new HashConfuse(strValue);
                    alst.Add(hshcnf1);
                    hash.Add(hshcnf1, strValue);
                }
            }

            Assert.True(fRetValue, "Test Failed");
        }

        [Fact]
        public void TestRemove02()
        {
            Hashtable hash = null;

            int ii;

            #region "Test Data"
            string[] strSuperHeroes = new string[]
            {
                "Captain Marvel" ,      //0
                "Batgirl" ,             //1
                "Nightwing" ,           //2
                "Green Lantern" ,       //3
                "Robin" ,               //4
                "Superman" ,            //5
                "Black Canary" ,        //6
                "Spiderman" ,           //7
                "Iron Man" ,            //8
                "Wonder Girl" ,        //9
                "Batman" ,              //10
                "Flash" ,               //11
                "Green Arrow" ,        //12
                "Atom" ,                //13
                "Steel" ,               //14
                "Powerman" ,            //15
            };

            string[] strSecretIdentities = new string[]
            {
                "Batson, Billy" ,       //0
                "Gordan, Barbara" ,     //1
                "Grayson, Dick" ,       //2
                "Jordan, Hal" ,     //3
                "Drake, Tim" ,          //4
                "Kent, Clark" ,     //5
                "Lance, Dinah" ,        //6
                "Parker, Peter" ,       //7
                "Stark, Tony" ,     //8
                "Troy, Donna" ,     //9
                "Wayne, Bruce" ,        //10
                "West, Wally" ,     //11
                "Queen, Oliver" ,       //12
                "Palmer, Ray" ,     //13
                "Irons, John Henry" ,   //14
                "Cage, Luke" ,          //15
            };
            #endregion

            // Allocate the hash table.
            hash = new Hashtable();

            // Construct the hash table by adding items to the table.
            for (ii = 0; ii < strSuperHeroes.Length; ++ii)
            {
                hash.Add(strSuperHeroes[ii], strSecretIdentities[ii]);
            }

            // Validate additions to Hashtable.
            Assert.Equal(strSuperHeroes.Length, hash.Count);

            //
            // []Remove: Attempt to remove a bogus key entry from table.
            //
            hash.Remove("THIS IS A BOGUS KEY");
            Assert.Equal(hash.Count, strSuperHeroes.Length);

            //
            // []	Remove: Attempt to remove a null key entry from table.
            //
            Assert.Throws<ArgumentNullException>(() =>
            {
                hash.Remove(null);
            }
            );

            //
            // [] Remove: Add key/value pair to Hashtable and remove items.
            //
            // Remove items from Hashtable.
            for (ii = 0; ii < strSuperHeroes.Length; ++ii)
            {
                hash.Remove(strSuperHeroes[ii]);
                Assert.Equal(strSuperHeroes.Length - ii - 1, hash.Count);
            }


            // now ADD ALL THE Entries the second time and remove them again
            for (ii = 0; ii < strSuperHeroes.Length; ++ii)
            {
                hash.Add(strSuperHeroes[ii], strSecretIdentities[ii]);
            }

            // remove elements
            for (ii = 0; ii < strSuperHeroes.Length; ++ii)
            {
                hash.Remove(strSuperHeroes[ii]);
                Assert.Equal(hash.Count, strSuperHeroes.Length - ii - 1);
            }

            //[]Repeated removed
            hash.Clear();
            for (int iAnnoying = 0; iAnnoying < 10; iAnnoying++)
            {
                for (ii = 0; ii < strSuperHeroes.Length; ++ii)
                {
                    hash.Remove(strSuperHeroes[ii]);
                    Assert.Equal(0, hash.Count);
                }
            }
        }
    }

    class HashConfuse
    {
        private string _strValue;

        public HashConfuse(string val)
        {
            _strValue = val;
        }

        public string Word
        {
            get { return _strValue; }
            set { _strValue = value; }
        }

        public override int GetHashCode()
        {
            return 5;
        }
    }
}
