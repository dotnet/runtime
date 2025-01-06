1 // Licensed to the .NET Foundation under one or more agreements.
2 // The .NET Foundation licenses this file to you under the MIT license.
3 
4 using System.Collections.Generic;
5 using Xunit;
6 
7 namespace System.ComponentModel.DataAnnotations.Tests
8 {
9     public class DisplayAttributeTests
10     {
11         [Fact]
12         public void Ctor()
13         {
14             DisplayAttribute attribute = new DisplayAttribute();
15             Assert.Null(attribute.ShortName);
16             Assert.Null(attribute.GetShortName());
17 
18             Assert.Null(attribute.Name);
19             Assert.Null(attribute.GetName());
20 
21             Assert.Null(attribute.Description);
22             Assert.Null(attribute.GetDescription());
23 
24             Assert.Null(attribute.Prompt);
25             Assert.Null(attribute.GetPrompt());
26 
27             Assert.Null(attribute.GroupName);
28             Assert.Null(attribute.GetGroupName());
29 
30             Assert.Null(attribute.ResourceType);
31         }
32 
33         [Fact]
34         public void AutoGenerateField_Get_NotSet_ThrowsInvalidOperationException()
35         {
36             DisplayAttribute attribute = new DisplayAttribute();
37             Assert.Throws<InvalidOperationException>(() => attribute.AutoGenerateField);
38             Assert.Null(attribute.GetAutoGenerateField());
39         }
40 
41         [Fact]
42         public void AutoGenerateFilter_Get_NotSet_ThrowsInvalidOperationException()
43         {
44             DisplayAttribute attribute = new DisplayAttribute();
45             Assert.Throws<InvalidOperationException>(() => attribute.AutoGenerateFilter);
46             Assert.Null(attribute.GetAutoGenerateFilter());
47         }
48 
49         [Fact]
50         public void Order_Get_NotSet_ThrowsInvalidOperationException()
51         {
52             DisplayAttribute attribute = new DisplayAttribute();
53             Assert.Throws<InvalidOperationException>(() => attribute.Order);
54             Assert.Null(attribute.GetOrder());
55         }
56 
57         public static IEnumerable<object[]> Strings_TestData()
58         {
59             yield return new object[] { "" };
60             yield return new object[] { " \r \t \n " };
61             yield return new object[] { "abc" };
62         }
63 
64         [Theory]
65         [MemberData(nameof(Strings_TestData))]
66         [InlineData("ShortName")]
67         public void ShortName_Get_Set(string value)
68         {
69             DisplayAttribute attribute = new DisplayAttribute();
70             attribute.ShortName = value;
71 
72             Assert.Equal(value, attribute.ShortName);
73             Assert.Equal(value == null, attribute.GetShortName() == null);
74 
75             // Set again, to cover the setter avoiding operations if the value is the same
76             attribute.ShortName = value;
77             Assert.Equal(value, attribute.ShortName);
78         }
79 
80         [Theory]
81         [MemberData(nameof(Strings_TestData))]
82         [InlineData("Name")]
83         public void Name_Get_Set(string value)
84         {
85             DisplayAttribute attribute = new DisplayAttribute();
86             attribute.Name = value;
87 
88             Assert.Equal(value, attribute.Name);
89             Assert.Equal(value == null, attribute.GetName() == null);
90 
91             // Set again, to cover the setter avoiding operations if the value is the same
92             attribute.Name = value;
93             Assert.Equal(value, attribute.Name);
94         }
95 
96         [Theory]
97         [MemberData(nameof(Strings_TestData))]
98         [InlineData("Description")]
99         public void Description_Get_Set(string value)
100         {
101             DisplayAttribute attribute = new DisplayAttribute();
102             attribute.Description = value;
103 
104             Assert.Equal(value, attribute.Description);
105             Assert.Equal(value == null, attribute.GetDescription() == null);
106 
107             // Set again, to cover the setter avoiding operations if the value is the same
108             attribute.Description = value;
109             Assert.Equal(value, attribute.Description);
110         }
111 
112         [Theory]
113         [MemberData(nameof(Strings_TestData))]
114         [InlineData("Prompt")]
115         public void Prompt_Get_Set(string value)
116         {
117             DisplayAttribute attribute = new DisplayAttribute();
118             attribute.Prompt = value;
119 
120             Assert.Equal(value, attribute.Prompt);
121             Assert.Equal(value == null, attribute.GetPrompt() == null);
122 
123             // Set again, to cover the setter avoiding operations if the value is the same
124             attribute.Prompt = value;
125             Assert.Equal(value, attribute.Prompt);
126         }
127 
128         [Theory]
129         [MemberData(nameof(Strings_TestData))]
130         [InlineData("GroupName")]
131         public void GroupName_Get_Set(string value)
132         {
133             DisplayAttribute attribute = new DisplayAttribute();
134             attribute.GroupName = value;
135 
136             Assert.Equal(value, attribute.GroupName);
137             Assert.Equal(value == null, attribute.GetGroupName() == null);
138 
139             // Set again, to cover the setter avoiding operations if the value is the same
140             attribute.GroupName = value;
141             Assert.Equal(value, attribute.GroupName);
142         }
143 
144         [Theory]
145         [InlineData(null)]
146         [InlineData(typeof(string))]
147         public void ResourceType_Get_Set(Type value)
148         {
149             DisplayAttribute attribute = new DisplayAttribute();
150             attribute.ResourceType = value;
151             Assert.Equal(value, attribute.ResourceType);
152 
153             // Set again, to cover the setter avoiding operations if the value is the same
154             attribute.ResourceType = value;
155             Assert.Equal(value, attribute.ResourceType);
156         }
157 
158         [Theory]
159         [InlineData(true)]
160         [InlineData(false)]
161         public void AutoGenerateField_Get_Set(bool value)
162         {
163             DisplayAttribute attribute = new DisplayAttribute();
164 
165             attribute.AutoGenerateField = value;
166             Assert.Equal(value, attribute.AutoGenerateField);
167             Assert.Equal(value, attribute.GetAutoGenerateField());
168         }
169 
170         [Theory]
171         [InlineData(true)]
172         [InlineData(false)]
173         public void AutoGenerateFilter_Get_Set(bool value)
174         {
175             DisplayAttribute attribute = new DisplayAttribute();
176 
177             attribute.AutoGenerateFilter = value;
178             Assert.Equal(value, attribute.AutoGenerateFilter);
179             Assert.Equal(value, attribute.GetAutoGenerateFilter());
180         }
181 
182         [Theory]
183         [InlineData(0)]
184         [InlineData(-1)]
185         [InlineData(1)]
186         public void Order_Get_Set(int value)
187         {
188             DisplayAttribute attribute = new DisplayAttribute();
189 
190             attribute.Order = value;
191             Assert.Equal(value, attribute.Order);
192             Assert.Equal(value, attribute.GetOrder());
193         }

194         [Fact]
195         public void LocalizableString_WorksWithInternalResourceType()
196         {
197             DisplayAttribute attribute = new DisplayAttribute();
198             attribute.ResourceType = typeof(InternalResourceType);
199             attribute.Name = nameof(InternalResourceType.InternalName);
200 
201             Assert.Equal(InternalResourceType.InternalName, attribute.GetName());
202         }

203         internal class InternalResourceType
204         {
205             internal static string InternalName => "Internal Resource Name";
206         }
207     }
208 }
