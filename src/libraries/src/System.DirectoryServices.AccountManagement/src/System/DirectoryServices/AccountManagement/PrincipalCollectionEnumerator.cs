/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    MemberCollectionEnumerator.cs

Abstract:

    Implements the PrincipalCollectionEnumerator class.

History:

    13-May-2004    MattRim     Created

--*/

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace System.DirectoryServices.AccountManagement
{
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.LinkDemand, Unrestricted = true)]
    internal class PrincipalCollectionEnumerator : IEnumerator<Principal>, IEnumerator
    {
        //
        // Public properties
        //

        public Principal Current
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="CheckDisposed():Void" />
            // </SecurityKernel>
            [System.Security.SecuritySafeCritical]
            get
            {
                CheckDisposed();

                // Since MoveNext() saved off the current value for us, this is largely trivial.

                if (this.endReached == true || this.currentMode == CurrentEnumeratorMode.None)
                {
                    // Either we're at the end or before the beginning
                    //  (CurrentEnumeratorMode.None implies we're _before_ the first value)

                    GlobalDebug.WriteLineIf(
                                        GlobalDebug.Warn, 
                                        "PrincipalCollectionEnumerator",
                                        "Current: bad position, endReached={0}, currentMode={1}",
                                        this.endReached,
                                        this.currentMode);

                    throw new InvalidOperationException(StringResources.PrincipalCollectionEnumInvalidPos);
                }

                Debug.Assert(this.current != null);
                return this.current;            
			}
        }


        object IEnumerator.Current
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="get_Current():Principal" />
            // <ReferencesCritical Name="Method: get_Current():Principal" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {
                return Current;
            }
        }


        //
        // Public methods
        //

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="CheckDisposed():Void" />
        // <SatisfiesLinkDemand Name="CheckChanged():Void" />
        // <SatisfiesLinkDemand Name="PrincipalCollection.get_Cleared():System.Boolean" />
        // <SatisfiesLinkDemand Name="PrincipalCollection.get_ClearCompleted():System.Boolean" />
        // <ReferencesCritical Name="Method: CheckChanged():Void" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecuritySafeCritical]
        public bool MoveNext()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "Entering MoveNext");    
        
            CheckDisposed();
            CheckChanged();

            // We previously reached the end, nothing more to do
            if (this.endReached)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: endReached");                
                return false;
            }
            
            lock (this.resultSet)
            {
                if (this.currentMode == CurrentEnumeratorMode.None)
                {
                    // At the very beginning

                    // In case this ResultSet was previously used with another PrincipalCollectionEnumerator instance
                    // (e.g., two foreach loops in a row)
                    this.resultSet.Reset();
                
                    if (!this.memberCollection.Cleared && !this.memberCollection.ClearCompleted)
                    {                
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: None mode, starting with existing values");                
                    
                        // Start by enumerating the existing values in the store
                        this.currentMode = CurrentEnumeratorMode.ResultSet;
                        this.enumerator  = null;
                    }
                    else
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: None mode, skipping existing values");                
                    
                        // The member collection was cleared.  Skip the ResultSet phase
                        this.currentMode = CurrentEnumeratorMode.InsertedValuesCompleted;
                        this.enumerator = (IEnumerator<Principal>) this.insertedValuesCompleted.GetEnumerator();                        
                    }
                }

                Debug.Assert(this.resultSet != null);

                if (this.currentMode == CurrentEnumeratorMode.ResultSet)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: ResultSet mode");                
                
                    bool needToRepeat = false;

                    do
                    {
                        bool f = this.resultSet.MoveNext();

                        if (f)
                        {
                            Principal principal = (Principal) this.resultSet.CurrentAsPrincipal;

                            if (this.removedValuesCompleted.Contains(principal) || this.removedValuesPending.Contains(principal))
                            {
                                // It's a value that's been removed (either a pending remove that hasn't completed, or a remove
                                // that completed _after_ we loaded the ResultSet from the store).    
                                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: ResultSet mode, found remove, skipping");
                                
                                needToRepeat = true;
                                continue;
                            }
                            else if (this.insertedValuesCompleted.Contains(principal) || this.insertedValuesPending.Contains(principal))
                            {
                                // insertedValuesCompleted: We must have gotten the ResultSet after the inserted committed.
                                // We don't want to return
                                // the principal twice, so we'll skip it here and later return it in 
                                // the CurrentEnumeratorMode.InsertedValuesCompleted mode.
                                //
                                // insertedValuesPending: The principal must have been originally in the ResultSet, but then
                                // removed, saved, and re-added, with the re-add still pending.
                                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: ResultSet mode, found insert, skipping");
                                
                                needToRepeat = true;
                                continue;
                            }
                            else
                            {
                                needToRepeat = false;
                                this.current = principal;
                                return true;
                            }   
                        }
                        else
                        {
                            // No more values left to retrieve.  Now try the insertedValuesCompleted list.
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: ResultSet mode, moving to InsValuesComp mode");
                            
                            this.currentMode = CurrentEnumeratorMode.InsertedValuesCompleted;
                            this.enumerator = (IEnumerator<Principal>) this.insertedValuesCompleted.GetEnumerator();
                            needToRepeat = false;
                        }
                    }
                    while (needToRepeat);
                }

                // These are values whose insertion has completed, but after we already loaded the ResultSet from the store.
                if (this.currentMode == CurrentEnumeratorMode.InsertedValuesCompleted)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: InsValuesComp mode");
                
                    bool f = this.enumerator.MoveNext();

                    if (f)
                    {
                        this.current = this.enumerator.Current;
                        return true;
                    }
                    else
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: InsValuesComp mode, moving to InsValuesPend mode");
                    
                        this.currentMode = CurrentEnumeratorMode.InsertedValuesPending;
                        this.enumerator = (IEnumerator<Principal>) this.insertedValuesPending.GetEnumerator();                        
                    }
                }

                // These are values whose insertion has not yet been committed to the store.
                if (this.currentMode == CurrentEnumeratorMode.InsertedValuesPending)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: InsValuesPend mode");
                
                    bool f = this.enumerator.MoveNext();

                    if (f)
                    {
                        this.current = this.enumerator.Current;
                        return true;
                    }
                    else
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: InsValuesPend mode, nothing left");
                    
                        this.endReached = true;
                        return false;                        
                    }
                }
                
            }

            Debug.Fail(String.Format(CultureInfo.CurrentCulture, "PrincipalCollectionEnumerator.MoveNext: fell off end of function, mode = {0}", this.currentMode.ToString()));
            return false;
        }

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="MoveNext():Boolean" />
        // <ReferencesCritical Name="Method: MoveNext():Boolean" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        bool IEnumerator.MoveNext()
        {
            return MoveNext();
        }

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="CheckDisposed():Void" />
        // <SatisfiesLinkDemand Name="CheckChanged():Void" />
        // <ReferencesCritical Name="Method: CheckChanged():Void" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        public void Reset()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "Reset");
        
            CheckDisposed();
            CheckChanged();

            // Set us up to start enumerating from the very beginning again
            this.endReached = false;
            this.enumerator = null;
            this.currentMode = CurrentEnumeratorMode.None;
        }


        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="Reset():Void" />
        // <ReferencesCritical Name="Method: Reset():Void" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        void IEnumerator.Reset()
        {
            Reset();
        }

        public void Dispose()   // IEnumerator<Principal> inherits from IDisposable
        {
            this.disposed = true;
        }


        //
        // Internal constructors
        //
        internal PrincipalCollectionEnumerator(
                                    ResultSet resultSet, 
                                    PrincipalCollection memberCollection, 
                                    List<Principal> removedValuesCompleted,
                                    List<Principal> removedValuesPending,
                                    List<Principal> insertedValuesCompleted,
                                    List<Principal> insertedValuesPending
                                    )
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "Ctor");    
        
            Debug.Assert(resultSet != null);
        
            this.resultSet = resultSet;
            this.memberCollection = memberCollection;
            this.removedValuesCompleted = removedValuesCompleted;
            this.removedValuesPending = removedValuesPending;
            this.insertedValuesCompleted = insertedValuesCompleted;
            this.insertedValuesPending = insertedValuesPending;
        }


        //
        // Private implementation
        //

        Principal current;

        // Remember: these are references to objects held by the PrincipalCollection class from which we came.
        // We don't own these, and shouldn't Dispose the ResultSet.
        //
        // SYNCHRONIZATION
        //   Access to:
        //      resultSet
        //   must be synchronized, since multiple enumerators could be iterating over us at once.
        //   Synchronize by locking on resultSet.

        ResultSet resultSet;
        List<Principal> insertedValuesPending;
        List<Principal> insertedValuesCompleted;
        List<Principal> removedValuesPending;
        List<Principal> removedValuesCompleted;

        bool endReached = false;    // true if there are no results left to iterate over

        IEnumerator<Principal> enumerator = null;   // The insertedValues{Completed,Pending} enumerator, used by MoveNext
        
        enum CurrentEnumeratorMode          // The set of values that MoveNext is currently iterating over
        {
            None,
            ResultSet,
            InsertedValuesCompleted,
            InsertedValuesPending
        }

        CurrentEnumeratorMode currentMode = CurrentEnumeratorMode.None;


        // To support IDisposable
        bool disposed = false;

        void CheckDisposed()
        {
            if (this.disposed)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "PrincipalCollectionEnumerator", "CheckDisposed: accessing disposed object");            
                throw new ObjectDisposedException("PrincipalCollectionEnumerator");
            }
        }   

        // When this enumerator was constructed, to detect changes made to the PrincipalCollection after it was constructed
        DateTime creationTime = DateTime.UtcNow;

        
        PrincipalCollection memberCollection = null;

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="PrincipalCollection.get_LastChange():System.DateTime" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        void CheckChanged()
        {
            // Make sure the app hasn't changed our underlying list
            if (this.memberCollection.LastChange > this.creationTime)
            {
                GlobalDebug.WriteLineIf(
                            GlobalDebug.Warn,
                            "PrincipalCollectionEnumerator", 
                            "CheckChanged: has changed (last change={0}, creation={1})",
                            this.memberCollection.LastChange,
                            this.creationTime);
            
                throw new InvalidOperationException(StringResources.PrincipalCollectionEnumHasChanged);  
            }
        }
        
    }
}

