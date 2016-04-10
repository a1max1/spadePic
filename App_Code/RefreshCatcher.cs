/*******************************************************************************
* Copyright 2009-2011 Amazon.com, Inc. or its affiliates. All Rights Reserved.
* 
* Licensed under the Apache License, Version 2.0 (the "License"). You may
* not use this file except in compliance with the License. A copy of the
* License is located at
* 
* http://aws.amazon.com/apache2.0/
* 
* or in the "license" file accompanying this file. This file is
* distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
* KIND, either express or implied. See the License for the specific
* language governing permissions and limitations under the License.
*******************************************************************************/

using System;
using System.Data;
using System.Configuration;
using System.Globalization;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

/// <summary>
/// RefreshCatcher is used to catch page refreshes 
/// (F5, back button, etc.) so that postbacks are not
/// processed when not intended.
/// Adapted from code at http://dotnetjunkies.com/WebLog/leon/archive/2006/02/23/135525.aspx 
/// </summary>
public class RefreshCatcher
{
    public RefreshCatcher(System.Web.UI.Page PageToCheck)
	{
        m_Page = PageToCheck;
        m_ctl_Refresh = (System.Web.UI.WebControls.HiddenField)PageToCheck.FindControl(REFRESH_NAME);
    }

    System.Web.UI.Page m_Page;
    private const string REFRESH_NAME = "__REFRESHSTAMP";
    private bool m_RefreshStamped;
    private bool m_IsRefresh;
    private System.Web.UI.WebControls.HiddenField m_ctl_Refresh;
    
    public bool IsRefresh
    {
      get
      {
         if (! m_RefreshStamped)
            m_IsRefresh = checkRefresh();
         return m_IsRefresh;
      }
   }

   public int getValue(string val)
   {
      if (val == null || val.Length == 0)
         return 0;
      return Int32.Parse(val, CultureInfo.InvariantCulture);
   }

    private bool checkRefresh()
    {
      m_RefreshStamped = true;
      int prevStamp = getValue(m_Page.Session[REFRESH_NAME] as string);
      int stamp = getValue(m_ctl_Refresh.Value);
      if (stamp > prevStamp) // Postback
      {
          m_Page.Session[REFRESH_NAME] = stamp.ToString(CultureInfo.InvariantCulture);
         return false;
      }
      return true;
    }
}
