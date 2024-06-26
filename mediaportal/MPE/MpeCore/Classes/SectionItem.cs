#region Copyright (C) 2005-2011 Team MediaPortal

// Copyright (C) 2005-2011 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System.Xml.Serialization;
using System.Collections.Generic;
using System.Text;

namespace MpeCore.Classes
{
  public class SectionItem
  {
    public SectionItem()
    {
      Params = new SectionParamCollection();
      IncludedGroups = new List<string>();
      Guid = System.Guid.NewGuid().ToString();
      ConditionGroup = string.Empty;
      Actions = new ActionItemCollection();
      WizardButtonsEnum = Classes.WizardButtonsEnum.BackNextCancel;
      Condition = ActionConditionEnum.None;
    }

    //public SectionItem(SectionItem obj)
    //{
    //    Name = obj.Name;
    //    Params = obj.Params;
    //    IncludedGroups = obj.IncludedGroups;
    //    PanelName = obj.PanelName;
    //    ConditionGroup = obj.ConditionGroup;
    //    Actons = obj.Actons;
    //}

    [XmlAttribute]
    public string Guid { get; set; }

    [XmlAttribute]
    public string Name { get; set; }

    public SectionParamCollection Params { get; set; }
    public ActionItemCollection Actions { get; set; }
    public List<string> IncludedGroups { get; set; }
    public string PanelName { get; set; }

    [XmlAttribute]
    public string ConditionGroup { get; set; }

    [XmlAttribute]
    public ActionConditionEnum Condition { get; set; }

    public WizardButtonsEnum WizardButtonsEnum { get; set; }

    public override string ToString()
    {
      return PanelName;
    }
  }
}