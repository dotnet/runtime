using System;
using System.Collections.Generic;

///<summary>
///System.EventHandler.EndInvoke(System.IAsyncResult)
///</summary>

public class EventHandlerEndInvoke
{
    //create a instance of HelperArgs that the event effected.
    public HelperArgs helperArgs = new HelperArgs(null);

    //this event return the changed object.
    public void setMessage(Object src, HelperArgs hArgs)
    {
        this.helperArgs = hArgs;
    }

    public static int Main()
    {
        EventHandlerEndInvoke testObj = new EventHandlerEndInvoke();
        TestLibrary.TestFramework.BeginTestCase("for Method of System.EventHandler");
        if (testObj.RunTests())
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("FAIL");
            return 0;
        }

    }

    public bool RunTests()
    {
        bool retVal = true;
        retVal = PosTest1() && retVal;


        return retVal;
    }

    #region Test Logic

    public bool PosTest1()
    {
        bool retVal = true;

        String temp = TestLibrary.Generator.GetString(-55, false, 1, 255);

        TestLibrary.TestFramework.BeginScenario("PosTest1:invoke the method EndInvoke");
        try
        {
            HelperEvent helperEvent = new HelperEvent();

            EventHandler<HelperArgs> eventHandler = new EventHandler<HelperArgs>(setMessage);

            IAsyncResult iAR = null;

            eventHandler.EndInvoke(iAR);
            
            TestLibrary.TestFramework.LogError("001", "Expected NotSupportedException exception");
            retVal = false;
        }
        catch (NotSupportedException)
        {
            // expected
        }     
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    #endregion
}

#region Helper Class
public class HelperArgs : EventArgs
{
    private String m_msg = "Helper EventArgs";

    public String message
    {
        get
        {
            return m_msg;
        }
        set
        {
            m_msg = value;
        }
    }

    public HelperArgs(String msg)
    {
        m_msg = msg;
    }
}

public class HelperEvent
{
    private String m_Desc;

    public event EventHandler<HelperArgs> setEvent;

    public String Desc
    {
        set
        {
            EventHandler<HelperArgs> setevent = setEvent;
            if (setevent != null)
            {
                setevent(this, new HelperArgs(value));
            }
            m_Desc = value;
        }
        get
        {
            return m_Desc;
        }
    }
}
#endregion
