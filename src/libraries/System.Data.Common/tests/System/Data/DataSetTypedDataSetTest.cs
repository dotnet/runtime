// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// Copyright (c) 2004 Mainsoft Co.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Xunit;
using System.ComponentModel;

using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Tests;
using System.Xml;
using System.Xml.Schema;
using System.IO;
using System.Globalization;

namespace System.Data.Tests
{
    public class DataSetTypedDataSetTest
    {
        private string _eventStatus = string.Empty;

        [Fact]
        public void TypedDataSet()
        {
            int i = 0;
            //check dataset constructor
            myTypedDataSet ds = null;
            DataSet unTypedDs = new DataSet();
            ds = new myTypedDataSet();
            Assert.False(ds == null);
            Assert.Equal(typeof(myTypedDataSet), ds.GetType());

            // fill dataset
            ds.ReadXml(new StringReader(
                @"<?xml version=""1.0"" standalone=""yes""?>
                <myTypedDataSet xmlns=""http://www.tempuri.org/myTypedDataSet.xsd"">
                  <Order_x0020_Details>
                    <OrderID>10250</OrderID>
                    <ProductID>41</ProductID>
                    <UnitPrice>7.7000</UnitPrice>
                    <Quantity>10</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10250</OrderID>
                    <ProductID>51</ProductID>
                    <UnitPrice>42.4000</UnitPrice>
                    <Quantity>35</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10250</OrderID>
                    <ProductID>65</ProductID>
                    <UnitPrice>16.8000</UnitPrice>
                    <Quantity>15</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10251</OrderID>
                    <ProductID>22</ProductID>
                    <UnitPrice>16.8000</UnitPrice>
                    <Quantity>6</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10251</OrderID>
                    <ProductID>57</ProductID>
                    <UnitPrice>15.6000</UnitPrice>
                    <Quantity>15</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10251</OrderID>
                    <ProductID>65</ProductID>
                    <UnitPrice>16.8000</UnitPrice>
                    <Quantity>20</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10252</OrderID>
                    <ProductID>20</ProductID>
                    <UnitPrice>64.8000</UnitPrice>
                    <Quantity>40</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10252</OrderID>
                    <ProductID>33</ProductID>
                    <UnitPrice>2.0000</UnitPrice>
                    <Quantity>25</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10252</OrderID>
                    <ProductID>60</ProductID>
                    <UnitPrice>27.2000</UnitPrice>
                    <Quantity>40</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10253</OrderID>
                    <ProductID>31</ProductID>
                    <UnitPrice>10.0000</UnitPrice>
                    <Quantity>20</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10253</OrderID>
                    <ProductID>39</ProductID>
                    <UnitPrice>14.4000</UnitPrice>
                    <Quantity>42</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10253</OrderID>
                    <ProductID>49</ProductID>
                    <UnitPrice>16.0000</UnitPrice>
                    <Quantity>40</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10254</OrderID>
                    <ProductID>24</ProductID>
                    <UnitPrice>3.6000</UnitPrice>
                    <Quantity>15</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10254</OrderID>
                    <ProductID>55</ProductID>
                    <UnitPrice>19.2000</UnitPrice>
                    <Quantity>21</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10254</OrderID>
                    <ProductID>74</ProductID>
                    <UnitPrice>8.0000</UnitPrice>
                    <Quantity>21</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10255</OrderID>
                    <ProductID>2</ProductID>
                    <UnitPrice>15.2000</UnitPrice>
                    <Quantity>20</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10255</OrderID>
                    <ProductID>16</ProductID>
                    <UnitPrice>13.9000</UnitPrice>
                    <Quantity>35</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10255</OrderID>
                    <ProductID>36</ProductID>
                    <UnitPrice>15.2000</UnitPrice>
                    <Quantity>25</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10255</OrderID>
                    <ProductID>59</ProductID>
                    <UnitPrice>44.0000</UnitPrice>
                    <Quantity>30</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10256</OrderID>
                    <ProductID>53</ProductID>
                    <UnitPrice>26.2000</UnitPrice>
                    <Quantity>15</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10256</OrderID>
                    <ProductID>77</ProductID>
                    <UnitPrice>10.4000</UnitPrice>
                    <Quantity>12</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10257</OrderID>
                    <ProductID>27</ProductID>
                    <UnitPrice>35.1000</UnitPrice>
                    <Quantity>25</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10257</OrderID>
                    <ProductID>39</ProductID>
                    <UnitPrice>14.4000</UnitPrice>
                    <Quantity>6</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10257</OrderID>
                    <ProductID>77</ProductID>
                    <UnitPrice>10.4000</UnitPrice>
                    <Quantity>15</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10258</OrderID>
                    <ProductID>2</ProductID>
                    <UnitPrice>15.2000</UnitPrice>
                    <Quantity>50</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10258</OrderID>
                    <ProductID>5</ProductID>
                    <UnitPrice>17.0000</UnitPrice>
                    <Quantity>65</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10258</OrderID>
                    <ProductID>32</ProductID>
                    <UnitPrice>25.6000</UnitPrice>
                    <Quantity>6</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10259</OrderID>
                    <ProductID>21</ProductID>
                    <UnitPrice>8.0000</UnitPrice>
                    <Quantity>10</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10259</OrderID>
                    <ProductID>37</ProductID>
                    <UnitPrice>20.8000</UnitPrice>
                    <Quantity>1</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10260</OrderID>
                    <ProductID>41</ProductID>
                    <UnitPrice>7.7000</UnitPrice>
                    <Quantity>16</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10260</OrderID>
                    <ProductID>57</ProductID>
                    <UnitPrice>15.6000</UnitPrice>
                    <Quantity>50</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10260</OrderID>
                    <ProductID>62</ProductID>
                    <UnitPrice>39.4000</UnitPrice>
                    <Quantity>15</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10260</OrderID>
                    <ProductID>70</ProductID>
                    <UnitPrice>12.0000</UnitPrice>
                    <Quantity>21</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10261</OrderID>
                    <ProductID>21</ProductID>
                    <UnitPrice>8.0000</UnitPrice>
                    <Quantity>20</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10261</OrderID>
                    <ProductID>35</ProductID>
                    <UnitPrice>14.4000</UnitPrice>
                    <Quantity>20</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10262</OrderID>
                    <ProductID>5</ProductID>
                    <UnitPrice>17.0000</UnitPrice>
                    <Quantity>12</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10262</OrderID>
                    <ProductID>7</ProductID>
                    <UnitPrice>24.0000</UnitPrice>
                    <Quantity>15</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10262</OrderID>
                    <ProductID>56</ProductID>
                    <UnitPrice>30.4000</UnitPrice>
                    <Quantity>2</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10263</OrderID>
                    <ProductID>16</ProductID>
                    <UnitPrice>13.9000</UnitPrice>
                    <Quantity>60</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10263</OrderID>
                    <ProductID>24</ProductID>
                    <UnitPrice>3.6000</UnitPrice>
                    <Quantity>28</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10263</OrderID>
                    <ProductID>30</ProductID>
                    <UnitPrice>20.7000</UnitPrice>
                    <Quantity>60</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10263</OrderID>
                    <ProductID>74</ProductID>
                    <UnitPrice>8.0000</UnitPrice>
                    <Quantity>36</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10264</OrderID>
                    <ProductID>2</ProductID>
                    <UnitPrice>15.2000</UnitPrice>
                    <Quantity>35</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10264</OrderID>
                    <ProductID>41</ProductID>
                    <UnitPrice>7.7000</UnitPrice>
                    <Quantity>25</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10265</OrderID>
                    <ProductID>17</ProductID>
                    <UnitPrice>31.2000</UnitPrice>
                    <Quantity>30</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10265</OrderID>
                    <ProductID>70</ProductID>
                    <UnitPrice>12.0000</UnitPrice>
                    <Quantity>20</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10266</OrderID>
                    <ProductID>12</ProductID>
                    <UnitPrice>30.4000</UnitPrice>
                    <Quantity>12</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10267</OrderID>
                    <ProductID>40</ProductID>
                    <UnitPrice>14.7000</UnitPrice>
                    <Quantity>50</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10267</OrderID>
                    <ProductID>59</ProductID>
                    <UnitPrice>44.0000</UnitPrice>
                    <Quantity>70</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10267</OrderID>
                    <ProductID>76</ProductID>
                    <UnitPrice>14.4000</UnitPrice>
                    <Quantity>15</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10268</OrderID>
                    <ProductID>29</ProductID>
                    <UnitPrice>99.0000</UnitPrice>
                    <Quantity>10</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10268</OrderID>
                    <ProductID>72</ProductID>
                    <UnitPrice>27.8000</UnitPrice>
                    <Quantity>4</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10269</OrderID>
                    <ProductID>33</ProductID>
                    <UnitPrice>2.0000</UnitPrice>
                    <Quantity>60</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10269</OrderID>
                    <ProductID>72</ProductID>
                    <UnitPrice>27.8000</UnitPrice>
                    <Quantity>20</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10270</OrderID>
                    <ProductID>36</ProductID>
                    <UnitPrice>15.2000</UnitPrice>
                    <Quantity>30</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Order_x0020_Details>
                    <OrderID>10270</OrderID>
                    <ProductID>43</ProductID>
                    <UnitPrice>36.8000</UnitPrice>
                    <Quantity>25</Quantity>
                    <Discount>5.0</Discount>
                  </Order_x0020_Details>
                  <Orders>
                    <OrderID>10250</OrderID>
                    <CustomerID>HANAR</CustomerID>
                    <EmployeeID>4</EmployeeID>
                    <OrderDate>1996-07-08T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-08-05T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-07-12T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10251</OrderID>
                    <CustomerID>VICTE</CustomerID>
                    <EmployeeID>3</EmployeeID>
                    <OrderDate>1996-07-08T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-08-05T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-07-15T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10252</OrderID>
                    <CustomerID>SUPRD</CustomerID>
                    <EmployeeID>4</EmployeeID>
                    <OrderDate>1996-07-09T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-08-06T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-07-11T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10253</OrderID>
                    <CustomerID>HANAR</CustomerID>
                    <EmployeeID>3</EmployeeID>
                    <OrderDate>1996-07-10T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-07-24T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-07-16T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10254</OrderID>
                    <CustomerID>CHOPS</CustomerID>
                    <EmployeeID>5</EmployeeID>
                    <OrderDate>1996-07-11T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-08-08T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-07-23T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10255</OrderID>
                    <CustomerID>RICSU</CustomerID>
                    <EmployeeID>9</EmployeeID>
                    <OrderDate>1996-07-12T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-08-09T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-07-15T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10256</OrderID>
                    <CustomerID>WELLI</CustomerID>
                    <EmployeeID>3</EmployeeID>
                    <OrderDate>1996-07-15T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-08-12T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-07-17T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10257</OrderID>
                    <CustomerID>HILAA</CustomerID>
                    <EmployeeID>4</EmployeeID>
                    <OrderDate>1996-07-16T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-08-13T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-07-22T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10258</OrderID>
                    <CustomerID>ERNSH</CustomerID>
                    <EmployeeID>1</EmployeeID>
                    <OrderDate>1996-07-17T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-08-14T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-07-23T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10259</OrderID>
                    <CustomerID>CENTC</CustomerID>
                    <EmployeeID>4</EmployeeID>
                    <OrderDate>1996-07-18T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-08-15T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-07-25T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10260</OrderID>
                    <CustomerID>OTTIK</CustomerID>
                    <EmployeeID>4</EmployeeID>
                    <OrderDate>1996-07-19T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-08-16T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-07-29T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10261</OrderID>
                    <CustomerID>QUEDE</CustomerID>
                    <EmployeeID>4</EmployeeID>
                    <OrderDate>1996-07-19T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-08-16T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-07-30T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10262</OrderID>
                    <CustomerID>RATTC</CustomerID>
                    <EmployeeID>8</EmployeeID>
                    <OrderDate>1996-07-22T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-08-19T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-07-25T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10263</OrderID>
                    <CustomerID>ERNSH</CustomerID>
                    <EmployeeID>9</EmployeeID>
                    <OrderDate>1996-07-23T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-08-20T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-07-31T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10264</OrderID>
                    <CustomerID>FOLKO</CustomerID>
                    <EmployeeID>6</EmployeeID>
                    <OrderDate>1996-07-24T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-08-21T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-08-23T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10265</OrderID>
                    <CustomerID>BLONP</CustomerID>
                    <EmployeeID>2</EmployeeID>
                    <OrderDate>1996-07-25T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-08-22T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-08-12T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10266</OrderID>
                    <CustomerID>WARTH</CustomerID>
                    <EmployeeID>3</EmployeeID>
                    <OrderDate>1996-07-26T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-09-06T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-07-31T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10267</OrderID>
                    <CustomerID>FRANK</CustomerID>
                    <EmployeeID>4</EmployeeID>
                    <OrderDate>1996-07-29T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-08-26T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-08-06T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10268</OrderID>
                    <CustomerID>GROSR</CustomerID>
                    <EmployeeID>8</EmployeeID>
                    <OrderDate>1996-07-30T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-08-27T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-08-02T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10269</OrderID>
                    <CustomerID>WHITC</CustomerID>
                    <EmployeeID>5</EmployeeID>
                    <OrderDate>1996-07-31T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-08-14T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-08-09T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                  <Orders>
                    <OrderID>10270</OrderID>
                    <CustomerID>WARTH</CustomerID>
                    <EmployeeID>1</EmployeeID>
                    <OrderDate>1996-08-01T00:00:00.0000000+03:00</OrderDate>
                    <RequiredDate>1996-08-29T00:00:00.0000000+03:00</RequiredDate>
                    <ShippedDate>1996-08-02T00:00:00.0000000+03:00</ShippedDate>
                  </Orders>
                </myTypedDataSet>"));

            // check DataSet named property "Orders"
            myTypedDataSet.OrdersDataTable tblOrders = null;
            tblOrders = ds.Orders;
            Assert.Equal(ds.Tables["Orders"], tblOrders);

            //check DataSet named property Orders - by index");
            tblOrders = ds.Orders;
            Assert.Equal(ds.Tables[1], tblOrders);

            //add new row AddTableNameRow, check row count");
            i = tblOrders.Rows.Count;
            tblOrders.AddOrdersRow("SAVEA", 1, new DateTime(1998, 05, 01, 00, 00, 00, 000)
                , new DateTime(1998, 05, 29, 00, 00, 00, 000)
                , new DateTime(1998, 05, 04, 00, 00, 00, 000), 1, 30.0900m
                , "Save-a-lot Markets", "187 Suffolk Ln.", "Boise", "ID", "83720", "USA");
            Assert.Equal(i + 1, tblOrders.Rows.Count);

            //check the new row AutoIncrement field - AddTableNameRow
            i = (int)tblOrders.Rows[tblOrders.Rows.Count - 2][0];
            Assert.Equal(i + 1, (int)tblOrders.Rows[tblOrders.Rows.Count - 1][0]);

            //Create New Row using NewTableNameRow, check row != null
            myTypedDataSet.OrdersRow drOrders = null;
            drOrders = tblOrders.NewOrdersRow();
            Assert.False(drOrders == null);

            //Create New Row using NewTableNameRow, check row state
            Assert.Equal(DataRowState.Detached, drOrders.RowState);

            //add new row NewTableNameRow, check row count
            //drOrders.OrderID = DBNull.Value;
            drOrders.CustomerID = "GREAL";
            drOrders.EmployeeID = 4;
            drOrders.OrderDate = new DateTime(1998, 04, 30, 00, 00, 00, 000);
            drOrders.RequiredDate = new DateTime(1998, 06, 11, 00, 00, 00, 000);
            drOrders["ShippedDate"] = DBNull.Value;
            drOrders.ShipVia = 3;
            drOrders.Freight = 14.0100m;
            drOrders.ShipName = "Great Lakes";
            drOrders.ShipAddress = "Food Market";
            drOrders.ShipCity = "Baker Blvd.";
            drOrders.ShipRegion = "Eugene";
            drOrders.ShipPostalCode = "OR	97403";
            drOrders.ShipCountry = "USA";

            i = tblOrders.Rows.Count;
            tblOrders.AddOrdersRow(drOrders);
            Assert.Equal(i + 1, tblOrders.Rows.Count);

            //check StrongTypingException
            Assert.Throws<StrongTypingException>(() =>
            {
                DateTime d = drOrders.ShippedDate; //drOrders.ShippedDate = null, will raise exception
            });

            //check the new row AutoIncrement field - NewTableNameRow
            i = (int)tblOrders.Rows[tblOrders.Rows.Count - 2][0];
            Assert.Equal(i + 1, (int)tblOrders.Rows[tblOrders.Rows.Count - 1][0]);

            // convenience IsNull functions
            // only if it can be null
            Assert.False(drOrders.IsShipAddressNull());

            drOrders.SetShipAddressNull();
            Assert.True(drOrders.IsShipAddressNull());

            // Table exposes a public property Count == table.Rows.Count
            Assert.Equal(tblOrders.Count, tblOrders.Rows.Count);


            // find function
            myTypedDataSet.OrdersRow dr = tblOrders[0];
            Assert.Equal(tblOrders.FindByOrderID(dr.OrderID), dr);

            //Remove row and check row count
            i = tblOrders.Count;
            myTypedDataSet.OrdersRow drr = tblOrders[0];
            tblOrders.RemoveOrdersRow(drr);
            Assert.Equal(i - 1, tblOrders.Count);

            //first column is readonly
            Assert.True(tblOrders.OrderIDColumn.ReadOnly);

            //read only exception
            Assert.Throws<ReadOnlyException>(() =>
            {
                tblOrders[0].OrderID = 99;
            });

            tblOrders.AcceptChanges();

            //Check table events
            // add event handlers
            ds.Orders.OrdersRowChanging += new myTypedDataSet.OrdersRowChangeEventHandler(T_Changing);
            ds.Orders.OrdersRowChanged += new myTypedDataSet.OrdersRowChangeEventHandler(T_Changed);
            ds.Orders.OrdersRowDeleting += new myTypedDataSet.OrdersRowChangeEventHandler(T_Deleting);
            ds.Orders.OrdersRowDeleted += new myTypedDataSet.OrdersRowChangeEventHandler(T_Deleted);

            //RowChange event order
            tblOrders[0].ShipCity = "Tel Aviv";
            Assert.Equal("AB", _eventStatus);

            _eventStatus = string.Empty;
            //RowDelet event order
            tblOrders[0].Delete();
            Assert.Equal("AB", _eventStatus);

            //expose DataColumn as property
            Assert.Equal(ds.Orders.OrderIDColumn, ds.Tables["Orders"].Columns["OrderID"]);

            //Accept changes for all deleted and changedd rows.
            ds.AcceptChanges();

            //check relations
            //ChildTableRow has property ParentTableRow
            myTypedDataSet.OrdersRow dr1 = ds.Order_Details[0].OrdersRow;
            DataRow dr2 = ds.Order_Details[0].GetParentRow(ds.Relations[0]);
            Assert.Equal(dr1, dr2);

            //ParentTableRow has property ChildTableRow
            myTypedDataSet.Order_DetailsRow[] drArr1 = ds.Orders[0].GetOrder_DetailsRows();
            DataRow[] drArr2 = ds.Orders[0].GetChildRows(ds.Relations[0]);
            Assert.Equal(drArr1, drArr2);

            //now test serialization of a typed dataset generated by microsoft's xsd.exe
            DataSet1 ds1 = new DataSet1();
            ds1.DataTable1.AddDataTable1Row("test");
            ds1.DataTable1.AddDataTable1Row("test2");

            DataSet1 ds1load = BinaryFormatterHelpers.Clone(ds1);

            Assert.True(ds1load.Tables.Contains("DataTable1"));
            Assert.Equal("DataTable1DataTable", ds1load.Tables["DataTable1"].GetType().Name);
            Assert.Equal(2, ds1load.DataTable1.Rows.Count);
            Assert.Equal("DataTable1Row", ds1load.DataTable1[0].GetType().Name);
            if (ds1load.DataTable1[0].Column1 == "test")
            {
                Assert.Equal("test2", ds1load.DataTable1[1].Column1);
            }
            else if (ds1load.DataTable1[0].Column1 == "test2")
            {
                Assert.Equal("test", ds1load.DataTable1[1].Column1);
            }
            else
            {
                Assert.False(true);
            }

            //now test when the mode is exclude schema
            ds1.SchemaSerializationMode = global::System.Data.SchemaSerializationMode.ExcludeSchema;

            ds1load = BinaryFormatterHelpers.Clone(ds1);

            Assert.True(ds1load.Tables.Contains("DataTable1"));
            Assert.Equal("DataTable1DataTable", ds1load.Tables["DataTable1"].GetType().Name);
            Assert.Equal(2, ds1load.DataTable1.Rows.Count);
            Assert.Equal("DataTable1Row", ds1load.DataTable1[0].GetType().Name);
            if (ds1load.DataTable1[0].Column1 == "test")
            {
                Assert.Equal("test2", ds1load.DataTable1[1].Column1);
            }
            else if (ds1load.DataTable1[0].Column1 == "test2")
            {
                Assert.Equal("test", ds1load.DataTable1[1].Column1);
            }
            else
            {
                Assert.False(true);
            }
        }

        protected void T_Changing(object sender, myTypedDataSet.OrdersRowChangeEvent e)
        {
            _eventStatus += "A";
        }

        protected void T_Changed(object sender, myTypedDataSet.OrdersRowChangeEvent e)
        {
            _eventStatus += "B";
        }

        protected void T_Deleting(object sender, myTypedDataSet.OrdersRowChangeEvent e)
        {
            _eventStatus += "A";
        }

        protected void T_Deleted(object sender, myTypedDataSet.OrdersRowChangeEvent e)
        {
            _eventStatus += "B";
        }

        [Serializable]
        [DesignerCategoryAttribute("code")]
        [ToolboxItem(true)]
        public class myTypedDataSet : DataSet
        {
            private Order_DetailsDataTable _tableOrder_Details;

            private OrdersDataTable _tableOrders;

            private DataRelation _relationOrdersOrder_x0020_Details;

            public myTypedDataSet()
            {
                InitClass();
                CollectionChangeEventHandler schemaChangedHandler = new CollectionChangeEventHandler(SchemaChanged);
                Tables.CollectionChanged += schemaChangedHandler;
                Relations.CollectionChanged += schemaChangedHandler;
            }

            protected myTypedDataSet(SerializationInfo info, StreamingContext context)
            {
                string strSchema = ((string)(info.GetValue("XmlSchema", typeof(string))));
                if ((strSchema != null))
                {
                    var ds = new DataSet();
                    ds.ReadXmlSchema(new XmlTextReader(new StringReader(strSchema)));
                    if ((ds.Tables["Order Details"] != null))
                    {
                        Tables.Add(new Order_DetailsDataTable(ds.Tables["Order Details"]));
                    }
                    if ((ds.Tables["Orders"] != null))
                    {
                        Tables.Add(new OrdersDataTable(ds.Tables["Orders"]));
                    }
                    DataSetName = ds.DataSetName;
                    Prefix = ds.Prefix;
                    Namespace = ds.Namespace;
                    Locale = ds.Locale;
                    CaseSensitive = ds.CaseSensitive;
                    EnforceConstraints = ds.EnforceConstraints;
                    Merge(ds, false, MissingSchemaAction.Add);
                    InitVars();
                }
                else
                {
                    InitClass();
                }
                GetSerializationData(info, context);
                CollectionChangeEventHandler schemaChangedHandler = new CollectionChangeEventHandler(SchemaChanged);
                Tables.CollectionChanged += schemaChangedHandler;
                Relations.CollectionChanged += schemaChangedHandler;
            }

            [Browsable(false)]
            [DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Content)]
            public Order_DetailsDataTable Order_Details
            {
                get
                {
                    return _tableOrder_Details;
                }
            }

            [Browsable(false)]
            [DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Content)]
            public OrdersDataTable Orders
            {
                get
                {
                    return _tableOrders;
                }
            }

            public override DataSet Clone()
            {
                myTypedDataSet cln = ((myTypedDataSet)(base.Clone()));
                cln.InitVars();
                return cln;
            }

            protected override bool ShouldSerializeTables()
            {
                return false;
            }

            protected override bool ShouldSerializeRelations()
            {
                return false;
            }

            protected override void ReadXmlSerializable(XmlReader reader)
            {
                Reset();
                var ds = new DataSet();
                ds.ReadXml(reader);
                if ((ds.Tables["Order Details"] != null))
                {
                    Tables.Add(new Order_DetailsDataTable(ds.Tables["Order Details"]));
                }
                if ((ds.Tables["Orders"] != null))
                {
                    Tables.Add(new OrdersDataTable(ds.Tables["Orders"]));
                }
                DataSetName = ds.DataSetName;
                Prefix = ds.Prefix;
                Namespace = ds.Namespace;
                Locale = ds.Locale;
                CaseSensitive = ds.CaseSensitive;
                EnforceConstraints = ds.EnforceConstraints;
                Merge(ds, false, MissingSchemaAction.Add);
                InitVars();
            }

            protected override XmlSchema GetSchemaSerializable()
            {
                MemoryStream stream = new MemoryStream();
                WriteXmlSchema(new XmlTextWriter(stream, null));
                stream.Position = 0;
                return XmlSchema.Read(new XmlTextReader(stream), null);
            }

            internal void InitVars()
            {
                _tableOrder_Details = ((Order_DetailsDataTable)(Tables["Order Details"]));
                if ((_tableOrder_Details != null))
                {
                    _tableOrder_Details.InitVars();
                }
                _tableOrders = ((OrdersDataTable)(Tables["Orders"]));
                if ((_tableOrders != null))
                {
                    _tableOrders.InitVars();
                }
                _relationOrdersOrder_x0020_Details = Relations["OrdersOrder_x0020_Details"];
            }

            private void InitClass()
            {
                DataSetName = "myTypedDataSet";
                Prefix = "";
                Namespace = "http://www.tempuri.org/myTypedDataSet.xsd";
                Locale = new CultureInfo("en-US");
                CaseSensitive = false;
                EnforceConstraints = true;
                _tableOrder_Details = new Order_DetailsDataTable();
                Tables.Add(_tableOrder_Details);
                _tableOrders = new OrdersDataTable();
                Tables.Add(_tableOrders);
                ForeignKeyConstraint fkc;
                fkc = new ForeignKeyConstraint("OrdersOrder_x0020_Details", new DataColumn[] {
                                                                                                 _tableOrders.OrderIDColumn}, new DataColumn[] {
                                                                                                                                                       _tableOrder_Details.OrderIDColumn});
                _tableOrder_Details.Constraints.Add(fkc);
                fkc.AcceptRejectRule = AcceptRejectRule.None;
                fkc.DeleteRule = Rule.Cascade;
                fkc.UpdateRule = Rule.Cascade;
                _relationOrdersOrder_x0020_Details = new DataRelation("OrdersOrder_x0020_Details", new DataColumn[] {
                                                                                                                            _tableOrders.OrderIDColumn}, new DataColumn[] {
                                                                                                                                                                                  _tableOrder_Details.OrderIDColumn}, false);
                Relations.Add(_relationOrdersOrder_x0020_Details);
            }

            private bool ShouldSerializeOrder_Details()
            {
                return false;
            }

            private bool ShouldSerializeOrders()
            {
                return false;
            }

            private void SchemaChanged(object sender, CollectionChangeEventArgs e)
            {
                if ((e.Action == CollectionChangeAction.Remove))
                {
                    InitVars();
                }
            }

            public delegate void Order_DetailsRowChangeEventHandler(object sender, Order_DetailsRowChangeEvent e);

            public delegate void OrdersRowChangeEventHandler(object sender, OrdersRowChangeEvent e);

            public class Order_DetailsDataTable : DataTable, IEnumerable
            {
                private DataColumn _columnOrderID;

                private DataColumn _columnProductID;

                private DataColumn _columnUnitPrice;

                private DataColumn _columnQuantity;

                private DataColumn _columnDiscount;

                internal Order_DetailsDataTable() :
                    base("Order Details")
                {
                    InitClass();
                }

                internal Order_DetailsDataTable(DataTable table) :
                    base(table.TableName)
                {
                    if ((table.CaseSensitive != table.DataSet.CaseSensitive))
                    {
                        CaseSensitive = table.CaseSensitive;
                    }
                    if ((table.Locale.ToString() != table.DataSet.Locale.ToString()))
                    {
                        Locale = table.Locale;
                    }
                    if ((table.Namespace != table.DataSet.Namespace))
                    {
                        Namespace = table.Namespace;
                    }
                    Prefix = table.Prefix;
                    MinimumCapacity = table.MinimumCapacity;
                    DisplayExpression = table.DisplayExpression;
                }

                [Browsable(false)]
                public int Count
                {
                    get
                    {
                        return Rows.Count;
                    }
                }

                internal DataColumn OrderIDColumn
                {
                    get
                    {
                        return _columnOrderID;
                    }
                }

                internal DataColumn ProductIDColumn
                {
                    get
                    {
                        return _columnProductID;
                    }
                }

                internal DataColumn UnitPriceColumn
                {
                    get
                    {
                        return _columnUnitPrice;
                    }
                }

                internal DataColumn QuantityColumn
                {
                    get
                    {
                        return _columnQuantity;
                    }
                }

                internal DataColumn DiscountColumn
                {
                    get
                    {
                        return _columnDiscount;
                    }
                }

                public Order_DetailsRow this[int index]
                {
                    get
                    {
                        return ((Order_DetailsRow)(Rows[index]));
                    }
                }

                public event Order_DetailsRowChangeEventHandler Order_DetailsRowChanged;

                public event Order_DetailsRowChangeEventHandler Order_DetailsRowChanging;

                public event Order_DetailsRowChangeEventHandler Order_DetailsRowDeleted;

                public event Order_DetailsRowChangeEventHandler Order_DetailsRowDeleting;

                public void AddOrder_DetailsRow(Order_DetailsRow row)
                {
                    Rows.Add(row);
                }

                public Order_DetailsRow AddOrder_DetailsRow(OrdersRow parentOrdersRowByOrdersOrder_x0020_Details, int ProductID, decimal UnitPrice, short Quantity, string Discount)
                {
                    Order_DetailsRow rowOrder_DetailsRow = ((Order_DetailsRow)(NewRow()));
                    rowOrder_DetailsRow.ItemArray = new object[] {
                                                                     parentOrdersRowByOrdersOrder_x0020_Details[0],
                                                                     ProductID,
                                                                     UnitPrice,
                                                                     Quantity,
                                                                     Discount};
                    Rows.Add(rowOrder_DetailsRow);
                    return rowOrder_DetailsRow;
                }

                public Order_DetailsRow FindByOrderIDProductID(int OrderID, int ProductID)
                {
                    return ((Order_DetailsRow)(Rows.Find(new object[] {
                                                                               OrderID,
                                                                               ProductID})));
                }

                public IEnumerator GetEnumerator()
                {
                    return Rows.GetEnumerator();
                }

                public override DataTable Clone()
                {
                    Order_DetailsDataTable cln = ((Order_DetailsDataTable)(base.Clone()));
                    cln.InitVars();
                    return cln;
                }

                protected override DataTable CreateInstance()
                {
                    return new Order_DetailsDataTable();
                }

                internal void InitVars()
                {
                    _columnOrderID = Columns["OrderID"];
                    _columnProductID = Columns["ProductID"];
                    _columnUnitPrice = Columns["UnitPrice"];
                    _columnQuantity = Columns["Quantity"];
                    _columnDiscount = Columns["Discount"];
                }

                private void InitClass()
                {
                    _columnOrderID = new DataColumn("OrderID", typeof(int), null, MappingType.Element);
                    Columns.Add(_columnOrderID);
                    _columnProductID = new DataColumn("ProductID", typeof(int), null, MappingType.Element);
                    Columns.Add(_columnProductID);
                    _columnUnitPrice = new DataColumn("UnitPrice", typeof(decimal), null, MappingType.Element);
                    Columns.Add(_columnUnitPrice);
                    _columnQuantity = new DataColumn("Quantity", typeof(short), null, MappingType.Element);
                    Columns.Add(_columnQuantity);
                    _columnDiscount = new DataColumn("Discount", typeof(string), null, MappingType.Element);
                    Columns.Add(_columnDiscount);
                    Constraints.Add(new UniqueConstraint("Constraint1", new DataColumn[] {
                                                                                                  _columnOrderID,
                                                                                                  _columnProductID}, true));
                    _columnOrderID.AllowDBNull = false;
                    _columnProductID.AllowDBNull = false;
                    _columnUnitPrice.AllowDBNull = false;
                    _columnQuantity.AllowDBNull = false;
                    _columnDiscount.ReadOnly = true;
                }

                public Order_DetailsRow NewOrder_DetailsRow()
                {
                    return ((Order_DetailsRow)(NewRow()));
                }

                protected override DataRow NewRowFromBuilder(DataRowBuilder builder)
                {
                    return new Order_DetailsRow(builder);
                }

                protected override Type GetRowType()
                {
                    return typeof(Order_DetailsRow);
                }

                protected override void OnRowChanged(DataRowChangeEventArgs e)
                {
                    base.OnRowChanged(e);
                    if ((Order_DetailsRowChanged != null))
                    {
                        Order_DetailsRowChanged(this, new Order_DetailsRowChangeEvent(((Order_DetailsRow)(e.Row)), e.Action));
                    }
                }

                protected override void OnRowChanging(DataRowChangeEventArgs e)
                {
                    base.OnRowChanging(e);
                    if ((Order_DetailsRowChanging != null))
                    {
                        Order_DetailsRowChanging(this, new Order_DetailsRowChangeEvent(((Order_DetailsRow)(e.Row)), e.Action));
                    }
                }

                protected override void OnRowDeleted(DataRowChangeEventArgs e)
                {
                    base.OnRowDeleted(e);
                    if ((Order_DetailsRowDeleted != null))
                    {
                        Order_DetailsRowDeleted(this, new Order_DetailsRowChangeEvent(((Order_DetailsRow)(e.Row)), e.Action));
                    }
                }

                protected override void OnRowDeleting(DataRowChangeEventArgs e)
                {
                    base.OnRowDeleting(e);
                    if ((Order_DetailsRowDeleting != null))
                    {
                        Order_DetailsRowDeleting(this, new Order_DetailsRowChangeEvent(((Order_DetailsRow)(e.Row)), e.Action));
                    }
                }

                public void RemoveOrder_DetailsRow(Order_DetailsRow row)
                {
                    Rows.Remove(row);
                }
            }

            public class Order_DetailsRow : DataRow
            {
                private Order_DetailsDataTable _tableOrder_Details;

                internal Order_DetailsRow(DataRowBuilder rb) :
                    base(rb)
                {
                    _tableOrder_Details = ((Order_DetailsDataTable)(Table));
                }

                public int OrderID
                {
                    get
                    {
                        return ((int)(this[_tableOrder_Details.OrderIDColumn]));
                    }
                    set
                    {
                        this[_tableOrder_Details.OrderIDColumn] = value;
                    }
                }

                public int ProductID
                {
                    get
                    {
                        return ((int)(this[_tableOrder_Details.ProductIDColumn]));
                    }
                    set
                    {
                        this[_tableOrder_Details.ProductIDColumn] = value;
                    }
                }

                public decimal UnitPrice
                {
                    get
                    {
                        return ((decimal)(this[_tableOrder_Details.UnitPriceColumn]));
                    }
                    set
                    {
                        this[_tableOrder_Details.UnitPriceColumn] = value;
                    }
                }

                public short Quantity
                {
                    get
                    {
                        return ((short)(this[_tableOrder_Details.QuantityColumn]));
                    }
                    set
                    {
                        this[_tableOrder_Details.QuantityColumn] = value;
                    }
                }

                public string Discount
                {
                    get
                    {
                        try
                        {
                            return ((string)(this[_tableOrder_Details.DiscountColumn]));
                        }
                        catch (InvalidCastException e)
                        {
                            throw new StrongTypingException("Cannot get value because it is DBNull.", e);
                        }
                    }
                    set
                    {
                        this[_tableOrder_Details.DiscountColumn] = value;
                    }
                }

                public OrdersRow OrdersRow
                {
                    get
                    {
                        return ((OrdersRow)(GetParentRow(Table.ParentRelations["OrdersOrder_x0020_Details"])));
                    }
                    set
                    {
                        SetParentRow(value, Table.ParentRelations["OrdersOrder_x0020_Details"]);
                    }
                }

                public bool IsDiscountNull()
                {
                    return IsNull(_tableOrder_Details.DiscountColumn);
                }

                public void SetDiscountNull()
                {
                    this[_tableOrder_Details.DiscountColumn] = DBNull.Value;
                }
            }

            public class Order_DetailsRowChangeEvent : EventArgs
            {
                private Order_DetailsRow _eventRow;

                private DataRowAction _eventAction;

                public Order_DetailsRowChangeEvent(Order_DetailsRow row, DataRowAction action)
                {
                    _eventRow = row;
                    _eventAction = action;
                }

                public Order_DetailsRow Row
                {
                    get
                    {
                        return _eventRow;
                    }
                }

                public DataRowAction Action
                {
                    get
                    {
                        return _eventAction;
                    }
                }
            }

            public class OrdersDataTable : DataTable, IEnumerable
            {
                private DataColumn _columnOrderID;

                private DataColumn _columnCustomerID;

                private DataColumn _columnEmployeeID;

                private DataColumn _columnOrderDate;

                private DataColumn _columnRequiredDate;

                private DataColumn _columnShippedDate;

                private DataColumn _columnShipVia;

                private DataColumn _columnFreight;

                private DataColumn _columnShipName;

                private DataColumn _columnShipAddress;

                private DataColumn _columnShipCity;

                private DataColumn _columnShipRegion;

                private DataColumn _columnShipPostalCode;

                private DataColumn _columnShipCountry;

                internal OrdersDataTable() :
                    base("Orders")
                {
                    InitClass();
                }

                internal OrdersDataTable(DataTable table) :
                    base(table.TableName)
                {
                    if ((table.CaseSensitive != table.DataSet.CaseSensitive))
                    {
                        CaseSensitive = table.CaseSensitive;
                    }
                    if ((table.Locale.ToString() != table.DataSet.Locale.ToString()))
                    {
                        Locale = table.Locale;
                    }
                    if ((table.Namespace != table.DataSet.Namespace))
                    {
                        Namespace = table.Namespace;
                    }
                    Prefix = table.Prefix;
                    MinimumCapacity = table.MinimumCapacity;
                    DisplayExpression = table.DisplayExpression;
                }

                [Browsable(false)]
                public int Count
                {
                    get
                    {
                        return Rows.Count;
                    }
                }

                internal DataColumn OrderIDColumn
                {
                    get
                    {
                        return _columnOrderID;
                    }
                }

                internal DataColumn CustomerIDColumn
                {
                    get
                    {
                        return _columnCustomerID;
                    }
                }

                internal DataColumn EmployeeIDColumn
                {
                    get
                    {
                        return _columnEmployeeID;
                    }
                }

                internal DataColumn OrderDateColumn
                {
                    get
                    {
                        return _columnOrderDate;
                    }
                }

                internal DataColumn RequiredDateColumn
                {
                    get
                    {
                        return _columnRequiredDate;
                    }
                }

                internal DataColumn ShippedDateColumn
                {
                    get
                    {
                        return _columnShippedDate;
                    }
                }

                internal DataColumn ShipViaColumn
                {
                    get
                    {
                        return _columnShipVia;
                    }
                }

                internal DataColumn FreightColumn
                {
                    get
                    {
                        return _columnFreight;
                    }
                }

                internal DataColumn ShipNameColumn
                {
                    get
                    {
                        return _columnShipName;
                    }
                }

                internal DataColumn ShipAddressColumn
                {
                    get
                    {
                        return _columnShipAddress;
                    }
                }

                internal DataColumn ShipCityColumn
                {
                    get
                    {
                        return _columnShipCity;
                    }
                }

                internal DataColumn ShipRegionColumn
                {
                    get
                    {
                        return _columnShipRegion;
                    }
                }

                internal DataColumn ShipPostalCodeColumn
                {
                    get
                    {
                        return _columnShipPostalCode;
                    }
                }

                internal DataColumn ShipCountryColumn
                {
                    get
                    {
                        return _columnShipCountry;
                    }
                }

                public OrdersRow this[int index]
                {
                    get
                    {
                        return ((OrdersRow)(Rows[index]));
                    }
                }

                public event OrdersRowChangeEventHandler OrdersRowChanged;

                public event OrdersRowChangeEventHandler OrdersRowChanging;

                public event OrdersRowChangeEventHandler OrdersRowDeleted;

                public event OrdersRowChangeEventHandler OrdersRowDeleting;

                public void AddOrdersRow(OrdersRow row)
                {
                    Rows.Add(row);
                }

                public OrdersRow AddOrdersRow(string CustomerID, int EmployeeID, DateTime OrderDate, DateTime RequiredDate, DateTime ShippedDate, int ShipVia, decimal Freight, string ShipName, string ShipAddress, string ShipCity, string ShipRegion, string ShipPostalCode, string ShipCountry)
                {
                    OrdersRow rowOrdersRow = ((OrdersRow)(NewRow()));
                    rowOrdersRow.ItemArray = new object[] {
                                                              null,
                                                              CustomerID,
                                                              EmployeeID,
                                                              OrderDate,
                                                              RequiredDate,
                                                              ShippedDate,
                                                              ShipVia,
                                                              Freight,
                                                              ShipName,
                                                              ShipAddress,
                                                              ShipCity,
                                                              ShipRegion,
                                                              ShipPostalCode,
                                                              ShipCountry};
                    Rows.Add(rowOrdersRow);
                    return rowOrdersRow;
                }

                public OrdersRow FindByOrderID(int OrderID)
                {
                    return ((OrdersRow)(Rows.Find(new object[] {
                                                                        OrderID})));
                }

                public IEnumerator GetEnumerator()
                {
                    return Rows.GetEnumerator();
                }

                public override DataTable Clone()
                {
                    OrdersDataTable cln = ((OrdersDataTable)(base.Clone()));
                    cln.InitVars();
                    return cln;
                }

                protected override DataTable CreateInstance()
                {
                    return new OrdersDataTable();
                }

                internal void InitVars()
                {
                    _columnOrderID = Columns["OrderID"];
                    _columnCustomerID = Columns["CustomerID"];
                    _columnEmployeeID = Columns["EmployeeID"];
                    _columnOrderDate = Columns["OrderDate"];
                    _columnRequiredDate = Columns["RequiredDate"];
                    _columnShippedDate = Columns["ShippedDate"];
                    _columnShipVia = Columns["ShipVia"];
                    _columnFreight = Columns["Freight"];
                    _columnShipName = Columns["ShipName"];
                    _columnShipAddress = Columns["ShipAddress"];
                    _columnShipCity = Columns["ShipCity"];
                    _columnShipRegion = Columns["ShipRegion"];
                    _columnShipPostalCode = Columns["ShipPostalCode"];
                    _columnShipCountry = Columns["ShipCountry"];
                }

                private void InitClass()
                {
                    _columnOrderID = new DataColumn("OrderID", typeof(int), null, MappingType.Element);
                    Columns.Add(_columnOrderID);
                    _columnCustomerID = new DataColumn("CustomerID", typeof(string), null, MappingType.Element);
                    Columns.Add(_columnCustomerID);
                    _columnEmployeeID = new DataColumn("EmployeeID", typeof(int), null, MappingType.Element);
                    Columns.Add(_columnEmployeeID);
                    _columnOrderDate = new DataColumn("OrderDate", typeof(DateTime), null, MappingType.Element);
                    Columns.Add(_columnOrderDate);
                    _columnRequiredDate = new DataColumn("RequiredDate", typeof(DateTime), null, MappingType.Element);
                    Columns.Add(_columnRequiredDate);
                    _columnShippedDate = new DataColumn("ShippedDate", typeof(DateTime), null, MappingType.Element);
                    Columns.Add(_columnShippedDate);
                    _columnShipVia = new DataColumn("ShipVia", typeof(int), null, MappingType.Element);
                    Columns.Add(_columnShipVia);
                    _columnFreight = new DataColumn("Freight", typeof(decimal), null, MappingType.Element);
                    Columns.Add(_columnFreight);
                    _columnShipName = new DataColumn("ShipName", typeof(string), null, MappingType.Element);
                    Columns.Add(_columnShipName);
                    _columnShipAddress = new DataColumn("ShipAddress", typeof(string), null, MappingType.Element);
                    Columns.Add(_columnShipAddress);
                    _columnShipCity = new DataColumn("ShipCity", typeof(string), null, MappingType.Element);
                    Columns.Add(_columnShipCity);
                    _columnShipRegion = new DataColumn("ShipRegion", typeof(string), null, MappingType.Element);
                    Columns.Add(_columnShipRegion);
                    _columnShipPostalCode = new DataColumn("ShipPostalCode", typeof(string), null, MappingType.Element);
                    Columns.Add(_columnShipPostalCode);
                    _columnShipCountry = new DataColumn("ShipCountry", typeof(string), null, MappingType.Element);
                    Columns.Add(_columnShipCountry);
                    Constraints.Add(new UniqueConstraint("Constraint1", new DataColumn[] {
                                                                                                  _columnOrderID}, true));
                    _columnOrderID.AutoIncrement = true;
                    _columnOrderID.AllowDBNull = false;
                    _columnOrderID.ReadOnly = true;
                    _columnOrderID.Unique = true;
                }

                public OrdersRow NewOrdersRow()
                {
                    return ((OrdersRow)(NewRow()));
                }

                protected override DataRow NewRowFromBuilder(DataRowBuilder builder)
                {
                    return new OrdersRow(builder);
                }

                protected override Type GetRowType()
                {
                    return typeof(OrdersRow);
                }

                protected override void OnRowChanged(DataRowChangeEventArgs e)
                {
                    base.OnRowChanged(e);
                    if ((OrdersRowChanged != null))
                    {
                        OrdersRowChanged(this, new OrdersRowChangeEvent(((OrdersRow)(e.Row)), e.Action));
                    }
                }

                protected override void OnRowChanging(DataRowChangeEventArgs e)
                {
                    base.OnRowChanging(e);
                    if ((OrdersRowChanging != null))
                    {
                        OrdersRowChanging(this, new OrdersRowChangeEvent(((OrdersRow)(e.Row)), e.Action));
                    }
                }

                protected override void OnRowDeleted(DataRowChangeEventArgs e)
                {
                    base.OnRowDeleted(e);
                    if ((OrdersRowDeleted != null))
                    {
                        OrdersRowDeleted(this, new OrdersRowChangeEvent(((OrdersRow)(e.Row)), e.Action));
                    }
                }

                protected override void OnRowDeleting(DataRowChangeEventArgs e)
                {
                    base.OnRowDeleting(e);
                    if ((OrdersRowDeleting != null))
                    {
                        OrdersRowDeleting(this, new OrdersRowChangeEvent(((OrdersRow)(e.Row)), e.Action));
                    }
                }

                public void RemoveOrdersRow(OrdersRow row)
                {
                    Rows.Remove(row);
                }
            }

            public class OrdersRow : DataRow
            {
                private OrdersDataTable _tableOrders;

                internal OrdersRow(DataRowBuilder rb) :
                    base(rb)
                {
                    _tableOrders = ((OrdersDataTable)(Table));
                }

                public int OrderID
                {
                    get
                    {
                        return ((int)(this[_tableOrders.OrderIDColumn]));
                    }
                    set
                    {
                        this[_tableOrders.OrderIDColumn] = value;
                    }
                }

                public string CustomerID
                {
                    get
                    {
                        try
                        {
                            return ((string)(this[_tableOrders.CustomerIDColumn]));
                        }
                        catch (InvalidCastException e)
                        {
                            throw new StrongTypingException("Cannot get value because it is DBNull.", e);
                        }
                    }
                    set
                    {
                        this[_tableOrders.CustomerIDColumn] = value;
                    }
                }

                public int EmployeeID
                {
                    get
                    {
                        try
                        {
                            return ((int)(this[_tableOrders.EmployeeIDColumn]));
                        }
                        catch (InvalidCastException e)
                        {
                            throw new StrongTypingException("Cannot get value because it is DBNull.", e);
                        }
                    }
                    set
                    {
                        this[_tableOrders.EmployeeIDColumn] = value;
                    }
                }

                public DateTime OrderDate
                {
                    get
                    {
                        try
                        {
                            return ((DateTime)(this[_tableOrders.OrderDateColumn]));
                        }
                        catch (InvalidCastException e)
                        {
                            throw new StrongTypingException("Cannot get value because it is DBNull.", e);
                        }
                    }
                    set
                    {
                        this[_tableOrders.OrderDateColumn] = value;
                    }
                }

                public DateTime RequiredDate
                {
                    get
                    {
                        try
                        {
                            return ((DateTime)(this[_tableOrders.RequiredDateColumn]));
                        }
                        catch (InvalidCastException e)
                        {
                            throw new StrongTypingException("Cannot get value because it is DBNull.", e);
                        }
                    }
                    set
                    {
                        this[_tableOrders.RequiredDateColumn] = value;
                    }
                }

                public DateTime ShippedDate
                {
                    get
                    {
                        try
                        {
                            return ((DateTime)(this[_tableOrders.ShippedDateColumn]));
                        }
                        catch (InvalidCastException e)
                        {
                            throw new StrongTypingException("Cannot get value because it is DBNull.", e);
                        }
                    }
                    set
                    {
                        this[_tableOrders.ShippedDateColumn] = value;
                    }
                }

                public int ShipVia
                {
                    get
                    {
                        try
                        {
                            return ((int)(this[_tableOrders.ShipViaColumn]));
                        }
                        catch (InvalidCastException e)
                        {
                            throw new StrongTypingException("Cannot get value because it is DBNull.", e);
                        }
                    }
                    set
                    {
                        this[_tableOrders.ShipViaColumn] = value;
                    }
                }

                public decimal Freight
                {
                    get
                    {
                        try
                        {
                            return ((decimal)(this[_tableOrders.FreightColumn]));
                        }
                        catch (InvalidCastException e)
                        {
                            throw new StrongTypingException("Cannot get value because it is DBNull.", e);
                        }
                    }
                    set
                    {
                        this[_tableOrders.FreightColumn] = value;
                    }
                }

                public string ShipName
                {
                    get
                    {
                        try
                        {
                            return ((string)(this[_tableOrders.ShipNameColumn]));
                        }
                        catch (InvalidCastException e)
                        {
                            throw new StrongTypingException("Cannot get value because it is DBNull.", e);
                        }
                    }
                    set
                    {
                        this[_tableOrders.ShipNameColumn] = value;
                    }
                }

                public string ShipAddress
                {
                    get
                    {
                        try
                        {
                            return ((string)(this[_tableOrders.ShipAddressColumn]));
                        }
                        catch (InvalidCastException e)
                        {
                            throw new StrongTypingException("Cannot get value because it is DBNull.", e);
                        }
                    }
                    set
                    {
                        this[_tableOrders.ShipAddressColumn] = value;
                    }
                }

                public string ShipCity
                {
                    get
                    {
                        try
                        {
                            return ((string)(this[_tableOrders.ShipCityColumn]));
                        }
                        catch (InvalidCastException e)
                        {
                            throw new StrongTypingException("Cannot get value because it is DBNull.", e);
                        }
                    }
                    set
                    {
                        this[_tableOrders.ShipCityColumn] = value;
                    }
                }

                public string ShipRegion
                {
                    get
                    {
                        try
                        {
                            return ((string)(this[_tableOrders.ShipRegionColumn]));
                        }
                        catch (InvalidCastException e)
                        {
                            throw new StrongTypingException("Cannot get value because it is DBNull.", e);
                        }
                    }
                    set
                    {
                        this[_tableOrders.ShipRegionColumn] = value;
                    }
                }

                public string ShipPostalCode
                {
                    get
                    {
                        try
                        {
                            return ((string)(this[_tableOrders.ShipPostalCodeColumn]));
                        }
                        catch (InvalidCastException e)
                        {
                            throw new StrongTypingException("Cannot get value because it is DBNull.", e);
                        }
                    }
                    set
                    {
                        this[_tableOrders.ShipPostalCodeColumn] = value;
                    }
                }

                public string ShipCountry
                {
                    get
                    {
                        try
                        {
                            return ((string)(this[_tableOrders.ShipCountryColumn]));
                        }
                        catch (InvalidCastException e)
                        {
                            throw new StrongTypingException("Cannot get value because it is DBNull.", e);
                        }
                    }
                    set
                    {
                        this[_tableOrders.ShipCountryColumn] = value;
                    }
                }

                public bool IsCustomerIDNull()
                {
                    return IsNull(_tableOrders.CustomerIDColumn);
                }

                public void SetCustomerIDNull()
                {
                    this[_tableOrders.CustomerIDColumn] = DBNull.Value;
                }

                public bool IsEmployeeIDNull()
                {
                    return IsNull(_tableOrders.EmployeeIDColumn);
                }

                public void SetEmployeeIDNull()
                {
                    this[_tableOrders.EmployeeIDColumn] = DBNull.Value;
                }

                public bool IsOrderDateNull()
                {
                    return IsNull(_tableOrders.OrderDateColumn);
                }

                public void SetOrderDateNull()
                {
                    this[_tableOrders.OrderDateColumn] = DBNull.Value;
                }

                public bool IsRequiredDateNull()
                {
                    return IsNull(_tableOrders.RequiredDateColumn);
                }

                public void SetRequiredDateNull()
                {
                    this[_tableOrders.RequiredDateColumn] = DBNull.Value;
                }

                public bool IsShippedDateNull()
                {
                    return IsNull(_tableOrders.ShippedDateColumn);
                }

                public void SetShippedDateNull()
                {
                    this[_tableOrders.ShippedDateColumn] = DBNull.Value;
                }

                public bool IsShipViaNull()
                {
                    return IsNull(_tableOrders.ShipViaColumn);
                }

                public void SetShipViaNull()
                {
                    this[_tableOrders.ShipViaColumn] = DBNull.Value;
                }

                public bool IsFreightNull()
                {
                    return IsNull(_tableOrders.FreightColumn);
                }

                public void SetFreightNull()
                {
                    this[_tableOrders.FreightColumn] = DBNull.Value;
                }

                public bool IsShipNameNull()
                {
                    return IsNull(_tableOrders.ShipNameColumn);
                }

                public void SetShipNameNull()
                {
                    this[_tableOrders.ShipNameColumn] = DBNull.Value;
                }

                public bool IsShipAddressNull()
                {
                    return IsNull(_tableOrders.ShipAddressColumn);
                }

                public void SetShipAddressNull()
                {
                    this[_tableOrders.ShipAddressColumn] = DBNull.Value;
                }

                public bool IsShipCityNull()
                {
                    return IsNull(_tableOrders.ShipCityColumn);
                }

                public void SetShipCityNull()
                {
                    this[_tableOrders.ShipCityColumn] = DBNull.Value;
                }

                public bool IsShipRegionNull()
                {
                    return IsNull(_tableOrders.ShipRegionColumn);
                }

                public void SetShipRegionNull()
                {
                    this[_tableOrders.ShipRegionColumn] = DBNull.Value;
                }

                public bool IsShipPostalCodeNull()
                {
                    return IsNull(_tableOrders.ShipPostalCodeColumn);
                }

                public void SetShipPostalCodeNull()
                {
                    this[_tableOrders.ShipPostalCodeColumn] = DBNull.Value;
                }

                public bool IsShipCountryNull()
                {
                    return IsNull(_tableOrders.ShipCountryColumn);
                }

                public void SetShipCountryNull()
                {
                    this[_tableOrders.ShipCountryColumn] = DBNull.Value;
                }

                public Order_DetailsRow[] GetOrder_DetailsRows()
                {
                    return ((Order_DetailsRow[])(GetChildRows(Table.ChildRelations["OrdersOrder_x0020_Details"])));
                }
            }

            public class OrdersRowChangeEvent : EventArgs
            {
                private OrdersRow _eventRow;

                private DataRowAction _eventAction;

                public OrdersRowChangeEvent(OrdersRow row, DataRowAction action)
                {
                    _eventRow = row;
                    _eventAction = action;
                }

                public OrdersRow Row
                {
                    get
                    {
                        return _eventRow;
                    }
                }

                public DataRowAction Action
                {
                    get
                    {
                        return _eventAction;
                    }
                }
            }
        }
    }
}
