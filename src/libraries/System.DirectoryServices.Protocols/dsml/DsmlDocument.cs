//------------------------------------------------------------------------------
// <copyright file=DsmlDocument.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

namespace System.DirectoryServices.Protocols {
    using System;
    using System.Collections;
    using System.Xml;
    using System.Diagnostics;
    using System.Net;
    using System.IO;
    using System.Text;
    using System.ComponentModel;

    public abstract class DsmlDocument
    {
        internal string dsmlRequestID = null;
        public abstract XmlDocument ToXml();        
    }

    public class DsmlRequestDocument: DsmlDocument, IList, IEnumerable
    {        
        private DsmlDocumentProcessing docProcessing = DsmlDocumentProcessing.Sequential;
        private DsmlResponseOrder resOrder= DsmlResponseOrder.Sequential;
        private DsmlErrorProcessing errProcessing = DsmlErrorProcessing.Exit;
        private ArrayList dsmlRequests;

        public DsmlRequestDocument() 
        {
            Utility.CheckOSVersion();
            
            dsmlRequests = new ArrayList();
        }

        public DsmlDocumentProcessing DocumentProcessing {
            get {
                return docProcessing;
            }
            set {
                if (value < DsmlDocumentProcessing.Sequential || value > DsmlDocumentProcessing.Parallel) 
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(DsmlDocumentProcessing));
            
                docProcessing = value;
            }
        }

        public DsmlResponseOrder ResponseOrder {
            get {
                return resOrder;
            }
            set {
                if (value < DsmlResponseOrder.Sequential || value > DsmlResponseOrder.Unordered) 
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(DsmlResponseOrder));
            
                resOrder = value;
            }
        }

        public DsmlErrorProcessing ErrorProcessing {
            get {
                return errProcessing;
            }
            set {
                if (value < DsmlErrorProcessing.Resume || value > DsmlErrorProcessing.Exit) 
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(DsmlErrorProcessing));
            
                errProcessing = value;
            }
        }

        public string RequestId {
            get {
                return dsmlRequestID;
            }
            set {
                dsmlRequestID = value;
            }
        }

        bool IList.IsFixedSize	
        {
            get
            {
                return false;
            }
        }

        bool IList.IsReadOnly
        {
            get		    
            {
                return false;
            } 
        }		

        object ICollection.SyncRoot
        {
            get		   
            {
                return dsmlRequests.SyncRoot;
            }
        }

        bool ICollection.IsSynchronized
        {
            get		
            {
                return dsmlRequests.IsSynchronized;
            }
        }        
		
        public IEnumerator GetEnumerator()
        {
            return dsmlRequests.GetEnumerator();
        }

        protected bool IsFixedSize	
        {
            get
            {
                return false;
            }
        }

        protected bool IsReadOnly
        {
            get		    
            {
                return false;
            } 
        }		

        protected object SyncRoot
        {
            get		   
            {
                return dsmlRequests.SyncRoot;
            }
        }

        protected bool IsSynchronized
        {
            get		
            {
                return dsmlRequests.IsSynchronized;
            }
        }

        public int Count
        {
            get			
            {
                return dsmlRequests.Count;
            }
        }

        int ICollection.Count
        {
            get			
            {
                return dsmlRequests.Count;
            }
        }        

        public DirectoryRequest this[int index] 
        {
            get
            { 
                return (DirectoryRequest) dsmlRequests[index];
            } 
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
			
                dsmlRequests[index] = value;
            }
        }	

        object IList.this[int index] 
        {
            get			
            { 
                return this[index];
            } 
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                if(!(value is DirectoryRequest))
                    throw new ArgumentException(Res.GetString(Res.InvalidValueType, "DirectoryRequest"), "value");
			
                dsmlRequests[index] = (DirectoryRequest) value;
            }
        }

        public int Add(DirectoryRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            return dsmlRequests.Add(request);
        }

        int IList.Add(object request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if(!(request is DirectoryRequest))
                throw new ArgumentException(Res.GetString(Res.InvalidValueType, "DirectoryRequest"), "request");

            return Add((DirectoryRequest)request);
        }

        public void Clear()
        {
            dsmlRequests.Clear();
        }        

        void IList.Clear()
        {
            Clear();
        }

        public bool Contains(DirectoryRequest value)
        {
            return dsmlRequests.Contains(value);
        }		

        bool IList.Contains(Object value)
        {
            return Contains((DirectoryRequest) value);
        }

        public int IndexOf(DirectoryRequest value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
	
            return dsmlRequests.IndexOf(value);
        }

        int IList.IndexOf(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
	
            return IndexOf((DirectoryRequest)value);
        }

        public void Insert(int index,DirectoryRequest value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
	
            dsmlRequests.Insert(index, value);
        }

        void IList.Insert(int index,object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            if(!(value is DirectoryRequest))
                throw new ArgumentException(Res.GetString(Res.InvalidValueType, "DirectoryRequest"), "value");
	
            Insert(index, (DirectoryRequest) value);
        }

        public void Remove(DirectoryRequest value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
	
            dsmlRequests.Remove(value);
        }		

        void IList.Remove(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
	
            Remove((DirectoryRequest)value);
        }

        public void RemoveAt(int index)
        {
            dsmlRequests.RemoveAt(index);
        }

        void IList.RemoveAt(int index)
        {
            RemoveAt(index);
        }

        public void CopyTo(DirectoryRequest[] value, int i)
        {  
            dsmlRequests.CopyTo(value, i);		
        }

        void ICollection.CopyTo(Array value, int i)
        {
            dsmlRequests.CopyTo(value, i);	
        }

        public override XmlDocument ToXml()
        {
            XmlDocument xmldoc = new XmlDocument();

            // create the batchRequest root element
            StartBatchRequest(xmldoc);

            // persist each operation under the batchRequest
            foreach (DirectoryRequest el in dsmlRequests)
            {
                xmldoc.DocumentElement.AppendChild(el.ToXmlNodeHelper(xmldoc));
            }

            return xmldoc;
        }

        private void StartBatchRequest(XmlDocument xmldoc)
        {
            //
            // Start with the common information
            // We'll make DSMLv2 the default namespace
            //
            string emptyLoad = "<batchRequest " + 
			                   "xmlns=\"" + DsmlConstants.DsmlUri +"\" " +
			                   "xmlns:xsd=\"" + DsmlConstants.XsdUri + "\" " +
			                   "xmlns:xsi=\"" + DsmlConstants.XsiUri + "\" />";
            xmldoc.LoadXml(emptyLoad); 


            //
            // Add in the DSML v2 processing directives
            //
            XmlAttribute attr;

            // DocumentProcessing
            attr = xmldoc.CreateAttribute("processing", null);
            switch (docProcessing)
            {
                case DsmlDocumentProcessing.Sequential:
                    attr.InnerText = "sequential";
                    break;

                case DsmlDocumentProcessing.Parallel:
                    attr.InnerText = "parallel";
                    break;

                default:
                    Debug.Assert(false, "Unknown DocumentProcessing type");
                    break;
            }

            xmldoc.DocumentElement.Attributes.Append(attr);

            // ResponseOrder
            attr = xmldoc.CreateAttribute("responseOrder", null);
            switch (resOrder)
            {
                case DsmlResponseOrder.Sequential:
                    attr.InnerText = "sequential";
                    break;

                case DsmlResponseOrder.Unordered:
                    attr.InnerText = "unordered";
                    break;

                default:
                    Debug.Assert(false, "Unknown ResponseOrder type");
                    break;
            }

            xmldoc.DocumentElement.Attributes.Append(attr);

            // ErrorProcessing
            attr = xmldoc.CreateAttribute("onError", null);
            switch (errProcessing)
            {
                case DsmlErrorProcessing.Exit:
                    attr.InnerText = "exit";
                    break;

                case DsmlErrorProcessing.Resume:
                    attr.InnerText = "resume";
                    break;

                default:
                    Debug.Assert(false, "Unknown ErrorProcessing type");
                    break;
            }

            xmldoc.DocumentElement.Attributes.Append(attr);

            //
            // RequestID
            //
            if (dsmlRequestID != null)
            {
                attr = xmldoc.CreateAttribute("requestID", null);
                attr.InnerText = dsmlRequestID;
                xmldoc.DocumentElement.Attributes.Append(attr);
            }
        }
		
    }

    public class DsmlResponseDocument : DsmlDocument, ICollection, IEnumerable {
        private ArrayList dsmlResponse;
        XmlDocument dsmlDocument = null;
        XmlElement dsmlBatchResponse = null;

        XmlNamespaceManager dsmlNS = null;

        private DsmlResponseDocument() 
        {
            dsmlResponse = new ArrayList();
        }

        internal DsmlResponseDocument(HttpWebResponse resp, string xpathToResponse) :this()
        {
            // Our caller (the DsmlConnection-derived class) passes us the XPath telling us
            // how to locate the batchResponse element.  This permits us to work with
            // different transport protocols.
        
            // Get the response stream
            Stream respStream = resp.GetResponseStream();
            StreamReader respStreamReader = new StreamReader(respStream);

            try
            {
                // Load the response from the stream into the XmlDocument
                dsmlDocument = new XmlDocument();

                try {
                    dsmlDocument.Load(respStreamReader);
                }
                catch (XmlException)
                {
                    // The server didn't return well-formed XML to us		
                    throw new DsmlInvalidDocumentException(Res.GetString(Res.NotWellFormedResponse));
                }
                
                // Locate the batchResponse element within the response document
                dsmlNS = NamespaceUtils.GetDsmlNamespaceManager();
                dsmlBatchResponse = (XmlElement) dsmlDocument.SelectSingleNode(xpathToResponse, dsmlNS);

                if (dsmlBatchResponse == null)
                {
                    throw new DsmlInvalidDocumentException(Res.GetString(Res.NotWellFormedResponse));
                }  

                // parse the response and put it in our internal store
                XmlNodeList nodeList = dsmlBatchResponse.ChildNodes;                

                // Unfortunately, we can't just index into the XmlNodeList,
                // because it's a list of all the nodes, not just the elements.
                // We're interested in the Nth element, not the Nth node.

                foreach (XmlNode node in nodeList)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        Debug.Assert(node is XmlElement);                    
                        
                        DirectoryResponse el = ConstructElement((XmlElement) node);
                        dsmlResponse.Add(el);                            
                    }
                }
            }
            finally
            {
                respStreamReader.Close();
            }
        }

        internal DsmlResponseDocument(StringBuilder responseString, string xpathToResponse) :this()
        {
            dsmlDocument = new XmlDocument();

            try {
                dsmlDocument.LoadXml(responseString.ToString());
            }
            catch (XmlException)
            {
                // The server didn't return well-formed XML to us		
                throw new DsmlInvalidDocumentException(Res.GetString(Res.NotWellFormedResponse));
            }

            // Locate the batchResponse element within the response document
            dsmlNS = NamespaceUtils.GetDsmlNamespaceManager();
            dsmlBatchResponse = (XmlElement) dsmlDocument.SelectSingleNode(xpathToResponse, dsmlNS);

    	    if (dsmlBatchResponse == null)
    	    {
    	        throw new DsmlInvalidDocumentException(Res.GetString(Res.NotWellFormedResponse));
    	    }  

    		// parse the response and put it in our internal store
    		XmlNodeList nodeList = dsmlBatchResponse.ChildNodes;                

            // Unfortunately, we can't just index into the XmlNodeList,
            // because it's a list of all the nodes, not just the elements.
            // We're interested in the Nth element, not the Nth node.

            foreach (XmlNode node in nodeList)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    Debug.Assert(node is XmlElement);
                    
                        
                    DirectoryResponse el = ConstructElement((XmlElement) node);
                    dsmlResponse.Add(el);                            
                }
            }
            
        }

        private DsmlResponseDocument(string responseString) :this(new StringBuilder(responseString), "se:Envelope/se:Body/dsml:batchResponse")
        {
        }

        public bool IsErrorResponse {
            get {
                // check whether there is a DsmlErrorResponse object
                foreach(DirectoryResponse res in dsmlResponse)
                {
                    if(res is DsmlErrorResponse)
                        return true;
                }

                return false;
            }
        }

        public bool IsOperationError {
            get {
                foreach(DirectoryResponse res in dsmlResponse)
                {
                    if(!(res is DsmlErrorResponse))
                    {
                        ResultCode result = res.ResultCode;
                        if (ResultCode.Success != result && 
                            ResultCode.CompareTrue != result &&
                            ResultCode.CompareFalse != result &&
                            ResultCode.Referral != result &&
                            ResultCode.ReferralV2 != result)
                        {
                            return true;
                        }
                            
                    }
                }

                return false;
            }
        }

        public string RequestId
        {
            get
            {
                // Locate the requestID attribute on the batchResponse element
                XmlAttribute attrReqID = (XmlAttribute) dsmlBatchResponse.SelectSingleNode("@dsml:requestID", dsmlNS);

                if (attrReqID == null)
                {
                    // try it without the namespace qualifier, in case the sender omitted it
                    attrReqID = (XmlAttribute) dsmlBatchResponse.SelectSingleNode("@requestID", dsmlNS);

                    if (attrReqID == null)
                    {
                        // server didn't return a requestID
                        return null;
                    }

                    return attrReqID.Value;
                }
                else
                {
                    return attrReqID.Value;
                }
            }            
        }

        internal string ResponseString {
            get {
                if(dsmlDocument != null)
                    return dsmlDocument.InnerXml;
                else
                    return null;
            }
        }

        // methods
        public override XmlDocument ToXml()
        {
            // returns a copy of the current document

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(dsmlBatchResponse.OuterXml);

            return doc;
        }       

        object ICollection.SyncRoot
        {
            get
            {
                return dsmlResponse.SyncRoot;
            }
        }

        bool ICollection.IsSynchronized
        {
            get		    
            {
                return dsmlResponse.IsSynchronized;
            }
        }

        int ICollection.Count
        {
            get
            {
                return dsmlResponse.Count;
            }
        }

        void ICollection.CopyTo(Array value, int i)
        {            
            dsmlResponse.CopyTo(value, i);		
        }
		
        public IEnumerator GetEnumerator()
        {
            return dsmlResponse.GetEnumerator();
        }

        protected object SyncRoot
        {
            get
            {
                return dsmlResponse.SyncRoot;
            }
        }

        protected bool IsSynchronized
        {
            get		    
            {
                return dsmlResponse.IsSynchronized;
            }
        }

        public int Count
        {
            get
            {
                return dsmlResponse.Count;
            }
        }		

        public DirectoryResponse this[int index] 
        {
            get
            { 
                return (DirectoryResponse) dsmlResponse[index];
            } 			
        }				

        public void CopyTo(DirectoryResponse[] value, int i)
        {            	
            dsmlResponse.CopyTo(value, i);		
        }		

        DirectoryResponse ConstructElement(XmlElement node)
        {
            DirectoryResponse el = null;

            Debug.Assert(node != null);
        
            switch (node.LocalName)
            {
                case DsmlConstants.DsmlErrorResponse:
                    el = new DsmlErrorResponse(node);
                    break;
                    
                case DsmlConstants.DsmlSearchResponse:
                    el = new SearchResponse(node);
                    break;
                
                case DsmlConstants.DsmlModifyResponse:
                    el = new ModifyResponse(node);
                    break;

                case DsmlConstants.DsmlAddResponse:
                    el = new AddResponse(node);
                    break;

                case DsmlConstants.DsmlDelResponse:
                    el = new DeleteResponse(node);
                    break;
                
                case DsmlConstants.DsmlModDNResponse:
                    el = new ModifyDNResponse(node);
                    break;
                
                case DsmlConstants.DsmlCompareResponse:
                    el = new CompareResponse(node);
                    break;
                
                case DsmlConstants.DsmlExtendedResponse:
                    el = new ExtendedResponse(node);
                    break;
                
                case DsmlConstants.DsmlAuthResponse:
                    el = new DsmlAuthResponse(node);
                    break;
                default:
                    throw new DsmlInvalidDocumentException(Res.GetString(Res.UnknownResponseElement));
            }

            Debug.Assert(el != null);

            return el;
        }

    }   

    
}
