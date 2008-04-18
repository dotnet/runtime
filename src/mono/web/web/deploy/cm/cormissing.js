// FIXME:
//	It still does not update icons previous to a type/member name when
//	certain icon kinds are unchecked (when an item has "todo" and "missing",
//	the default display is "missing" and when "missing" is unchecked it
//	should turn into "todo").

function toggle (elt)
{
	if (elt == null)
		return;

	var eltLink = firstElement (elt);
	if (eltLink != null && eltLink.className == 't')	// toggle
	{
		var ich = elt.className.indexOf ('_');
		if (ich < 0)
		{
			eltLink.src = 'cm/tp.gif';
			elt.className += '_';
		}
		else
		{
			eltLink.src = 'cm/tm.gif';
			elt.className = elt.className.slice (0, ich);
		}
	}
}

function setView (elt, fView)
{
	var eltLink = firstElement (elt);
	if (eltLink != null && eltLink.className == 't')	// toggle
	{
		var ich = elt.className.indexOf ('_');
		if (ich < 0 && !fView)
		{
			eltLink.src = 'cm/tp.gif';
			elt.className += '_';
		}
		else if (ich >= 0 && fView)
		{
			eltLink.src = 'cm/tm.gif';
			elt.className = elt.className.slice (0, ich);
		}
	}
}

function firstElement (elt)
{
	var c = elt.firstChild;
	while (c != null) {
		if (c.nodeType == 1) // Node.ELEMENT_NODE (IE6 does not recognize it)
			return c;
		c = c.nextSibling;
	}
	return null;
}

function trimSrc (strSrc)
{
	return strSrc.slice (strSrc.lastIndexOf ('/') + 1, strSrc.lastIndexOf ('.'));
}

function getChildrenByTagName (elt, strTag)
{
	strTag = strTag.toLowerCase ();
	var rgChildren = new Array ();
	var eltChild = firstElement (elt);
	while (eltChild)
	{
		if (eltChild.tagName && eltChild.tagName.toLowerCase () == strTag)
			rgChildren.push (eltChild);
		eltChild = eltChild.nextSibling;
	}
	return rgChildren;
}

function viewAll (elt, dictTypes, attrFilters)
{
	var fView = isShown (elt, dictTypes, attrFilters);

	var aCounts = new Array (4);
	for (i = 0; i < 4; i++)
		aCounts [i] = 0;
	var rgElts = getChildrenByTagName (elt, 'DIV');
	for (iElt in rgElts) {
		var aChildRet = viewAll (rgElts [iElt], dictTypes, attrFilters);
		if (aChildRet != null) {
			fView = true;
			for (i = 0; i < 4; i++)
				aCounts [i] += aChildRet [i];
		}
	}

	elt.style.display = fView ? '' : 'none';

	if (!fView)
		return null;

	rgShownDivs = getChildrenByTagName (elt, 'DIV');
	for (i = 0; i < rgShownDivs.length; i++) {
		var cDiv = rgShownDivs [i];
		if (cDiv.style.display == 'none')
			continue;
		incrementCount (cDiv, aCounts, dictTypes);
	}

	// update the numbers
	rgSpans = getChildrenByTagName (elt, 'SPAN');
	for (iSpan in rgSpans) {
		var cSpan = rgSpans [iSpan];
		var cImage = firstElement (cSpan);
		if (cImage == null)
			continue;
		switch (trimSrc (cImage.src)) {
		case 'st': cSpan.lastChild.nodeValue = ": " + aCounts [0]; break;
		case 'sm': cSpan.lastChild.nodeValue = ": " + aCounts [1]; break;
		case 'sx': cSpan.lastChild.nodeValue = ": " + aCounts [2]; break;
		case 'se': cSpan.lastChild.nodeValue = ": " + aCounts [3]; break;
		}
	}
	return aCounts;
}

function isShown (elt, dictTypes, attrFilters)
{
	if (!isShownMarkType (elt, dictTypes))
		return false;

	// Check attributes that are being filtered out.
	var rgSpans = getChildrenByTagName (elt, 'SPAN');
	var cSpans = rgSpans.length;
	for (var iSpan = 0; iSpan < cSpans; iSpan++)
	{
		var strSpan = rgSpans [iSpan].firstChild.nodeValue;
		for (strzzz in attrFilters)
			if (strSpan == strzzz)
				return false;
	}
	return true;
}

function isShownMarkType (elt, dictTypes)
{
	var rgImages = getChildrenByTagName (elt, 'IMG');
	var cImages = rgImages.length;
	for (var iImage = 0; iImage < cImages; iImage++)
	{
		var strImage = trimSrc (rgImages [iImage].src);
		if (dictTypes [strImage])
			return true;
	}
	return false;
}

function incrementCount (cDiv, aCounts, dictTypes)
{
	switch (cDiv.className) {
	case 'y': case 'y_': // assembly
	case 'n': case 'n_': // namespace
	// types
	case 'c': case 'c_': case 'i': case 'i_':
	case 'en': case 'en_': case 'd': case 'd_':
	// members
	case 'r': case 'r_': case 'x': case 'x_': case 'm': case 'm_':
	case 'f': case 'f_': case 'e': case 'e_': case 'p': 	case 'p_':
	case 'o': case 'o_': case 'a': case 'a_':
		var rgImgs = getChildrenByTagName (cDiv, 'IMG');
		for (iImg = 0; iImg < rgImgs.length; iImg++) {
			var cImg = rgImgs [iImg];
			if (cImg.className != 't')
				continue;
			var stype = trimSrc (cImg.src);
			if (!dictTypes [stype])
				continue;
			switch (stype) {
			case "st": aCounts [0]++; break;
			case "sm": aCounts [1]++; break;
			case "sx": aCounts [2]++; break;
			case "se": aCounts [3]++; break;
			default:
				continue;
			}
			break;
		}
		break;
	}
}

/* just for debugging use now.
function firstInnerText (elt)
{
	var s = elt.innerText;
	if (s != null)
		return s;
	var n = elt.firstChild;
	while (n != null) {
		s = n.nodeValue;
		if (s != null && s.replace (/^\s+/g, '') != '')
			return s;
		s = firstInnerText (n);
		if (s != null)
			return s;
		n = n.nextSibling;
	}
	return s;
}
*/

function getView (elt)
{
	var eltLink = firstElement (elt);
	if (eltLink != null && eltLink.className == 't')	// toggle
	{
		var ich = elt.className.indexOf ('_');
		if (ich < 0)
			return true;
	}
	return false;
}

function getParentDiv (elt)
{
	if (elt)
	{
		do
		{
			elt = elt.parentNode;
		}
		while (elt && elt.tagName != 'DIV');
	}

	return elt;
}

function getName (elt)
{
	var rgSpans = getChildrenByTagName (elt, 'SPAN');
	for (var iSpan = 0; iSpan < rgSpans.length; iSpan ++)
	{
		var span = rgSpans [iSpan];
		if (span.className == 'l')	// label
		{
			if (span.innerText)
				return span.innerText;
			else
				return span.firstChild.nodeValue;
		}
	}
	return null;
}

function clickHandler (evt)
{
	var elt;
	if (document.layers)
		elt = evt.taget;
	else if (window.event && window.event.srcElement)
	{
		elt = window.event.srcElement;
		evt = window.event;
	}
	else if (evt && evt.stopPropagation)
		elt = evt.target;
	
	if (!elt.className && elt.parentNode)
		elt = elt.parentNode;

	if (elt.className == 'l')	// label
	{
		var strClass;
		var strField;
		var strNamespace;
		var strAssembly;
		var strFieldType;

		elt = getParentDiv (elt);
		var strEltClass = elt.className;
		if (strEltClass.charAt (strEltClass.length - 1) == '_')
			strEltClass = strEltClass.slice (0, strEltClass.length - 1);

		if (strEltClass == 'x')	// constructor
		{
			strField = 'ctor';
			elt = getParentDiv (elt);
		}
		else
		if (strEltClass == 'm' ||	// method
			strEltClass == 'p' ||	// property
			strEltClass == 'e' ||	// event
			strEltClass == 'f')	// field
		{
			strFieldType = strEltClass;
			strField = getName (elt);
			var match = strField.match ( /[\.A-Z0-9_]*/i );
			if (match)
				strField = match [0];
			elt = getParentDiv (elt);

		}

		var strEltClass = elt.className;
		if (strEltClass.charAt (strEltClass.length - 1) == '_')
			strEltClass = strEltClass.slice (0, strEltClass.length - 1);

		if (strEltClass == 'c' ||	// class
			strEltClass == 's' ||	// struct
			strEltClass == 'i' ||	// struct
			strEltClass == 'd' ||	// delegate
			strEltClass == 'en')	// enum
		{
			strClass = getName (elt);
			if (strEltClass == 'en')
				strField = null;
			elt = getParentDiv (elt);
		}

		var strEltClass = elt.className;
		if (strEltClass.charAt (strEltClass.length - 1) == '_')
			strEltClass = strEltClass.slice (0, strEltClass.length - 1);

		if (strEltClass == 'n')
		{
			strNamespace = getName (elt);
			elt = getParentDiv (elt);
		}

		var strEltClass = elt.className;
		if (strEltClass.charAt (strEltClass.length - 1) == '_')
			strEltClass = strEltClass.slice (0, strEltClass.length - 1);

		if (strEltClass == 'y')
		{
			strAssembly = getName (elt);
		}

		if (evt.ctrlKey)
		{
			var strRoot = 'http://anonsvn.mono-project.com/viewcvs/trunk/mcs/class/';
			var strExtra = '';

			if (strAssembly)
			{
				if (strAssembly == 'mscorlib')
					strAssembly = 'corlib';
				else if (strAssembly == 'System.Xml')
					strAssembly = 'System.XML';

				strRoot = strRoot + strAssembly + '/';
				if (strNamespace)
				{
					strRoot = strRoot + strNamespace + '/';
					if (strClass)
					{
						strRoot += strClass + '.cs';
						strExtra += '?view=markup';
					}
				}
				window.open (strRoot + strExtra, 'CVS');
			}
		}
		else if (strNamespace)
		{
			if (document.getElementById ('TargetMsdn1').checked)
			{
				var re = /\./g ;
				strNamespace = strNamespace.toLowerCase ().replace (re, '');
				if (strClass)
					strNamespace += strClass.toLowerCase () + 'class';
				if (strField)
					strNamespace += strField;
				if (strClass || strField)
					strNamespace += 'topic';

				window.open ('http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpref/html/frlrf' + strNamespace + '.asp', 'MSDN');

			}
			else
			{
				if (strClass)
					strNamespace += '.' + strClass;
				if (strField)
					strNamespace += '.' + strField;
				if (document.getElementById ('TargetMonodoc').checked)
				{
					var category = null;
					if (strClass == null)
						category = "N:";
					else if (strField == null)
						category = "T:";
					else {
						switch (strFieldType) {
						case 'f': category = "F:"; break;
						case 'p': category = "P:"; break;
						case 'm': category = "M:"; break;
						case 'e': category = "E:"; break;
						}
					}
					if (category != null)
						window.open ('http://www.go-mono.com/docs/monodoc.ashx?link=' + category + strNamespace);
				}
				else
				{
					window.open ('http://msdn2.microsoft.com/library/' + strNamespace + '.aspx', 'MSDN');
				}
			}
		}
	}
	else
	{
		if (elt.parentNode && elt.parentNode.className == 't')	// toggle
			elt = elt.parentNode;
		else if (elt.className != 't')	// toggle
			return;

		while (elt != null && elt.tagName != 'DIV')
			elt = elt.parentNode;
		
		if (evt.shiftKey)
		{
			var rgElts = getChildrenByTagName (elt, 'DIV');
			var cElts = rgElts.length;
			if (cElts != 0)
			{
				var fView = false;
				var iElt;
				for (iElt = 0; iElt < cElts; iElt ++)
				{
					if (getView (rgElts [iElt]))
					{
						fView = true;
						break;
					}
				}
				for (iElt = 0; iElt < cElts; iElt ++)
				{
					setView (rgElts [iElt], !fView);
				}
			}
		}
		else if (evt.ctrlKey)
		{
			setView (elt, true);
			var eltParent = getParentDiv (elt);
			while (eltParent)
			{
				var rgSiblings = getChildrenByTagName (eltParent, 'DIV');
				var cSiblings = rgSiblings.length;
				for (var iSibling = 0; iSibling < cSiblings; iSibling++)
				{
					var eltSibling = rgSiblings [iSibling];
					if (eltSibling != elt)
					{
						setView (eltSibling, false);
					}
				}
				elt = eltParent;
				eltParent = getParentDiv (elt);
			}
		}
		else
			toggle (elt);
	}

	return false;
}

function filterTree ()
{
	var eltMissing = document.getElementById ('missing');
	var eltTodo = document.getElementById ('todo');
	var eltExtra = document.getElementById ('extra');
	var eltErrors = document.getElementById ('errors');
	var eltComVisible = document.getElementById ('ComVisible');
	var eltDebuggerDisplay = document.getElementById ('DebuggerDisplay');

	var dictTypes = new Object ();
	if (eltMissing.checked)
		dictTypes ['sm'] = true;
	if (eltTodo.checked)
		dictTypes ['st'] = true;
	if (eltErrors.checked)
		dictTypes ['se'] = true;
	if (eltExtra.checked)
		dictTypes ['sx'] = true;
//	dictTypes ['sc'] = true;

	var attrFilters = new Object ();
	var rgOptions = getChildrenByTagName (document.getElementById ('FilteredAttributes'), "option");
	for (i = 0; i < rgOptions.length; i++)
		attrFilters [rgOptions [i].firstChild.nodeValue.replace (/\s+/g, '')] = true;
	viewAll (document.getElementById ('ROOT'), dictTypes, attrFilters);
}

function addAndFilter ()
{
	var newInput = document.getElementById ('NewFilterTarget');
	var newAttr = newInput.value;
	if (newAttr.length > 0) {
		var selection = document.getElementById ('FilteredAttributes');
		var newElem = document.createElement ('option');
		newElem.appendChild (document.createTextNode (newAttr));
		selection.appendChild (newElem);
		newInput.value = '';
		filterTree ();
	}
}

function removeAndFilter ()
{
	var selection = document.getElementById ('FilteredAttributes');
	if (selection.selectedIndex >= 0) {
		var newInput = document.getElementById ('NewFilterTarget');
		if (newInput.value.length == 0)
			newInput.value = selection.options [selection.selectedIndex].firstChild.nodeValue;
		selection.removeChild (selection.options [selection.selectedIndex]);
		filterTree ();
	}
}

function selectMissing ()
{
	toggleFilter ('missing');
}

function selectTodo ()
{
	toggleFilter ('todo');
}

function selectExtra ()
{
	toggleFilter ('extra');
}

function selectErrors ()
{
	toggleFilter ('errors');
}

function toggleAttributeFilter (attrName)
{
	toggleFilter (attrName);
}

function toggleFilter (strFilter)
{
	var eltTodo = document.getElementById ('todo');
	var eltMissing = document.getElementById ('missing');
	var eltExtra = document.getElementById ('extra');
	var eltErrors = document.getElementById ('errors');

	var eltToggle = document.getElementById (strFilter);
	if (window && window.event && window.event.shiftKey)
	{
		eltMissing.checked = eltTodo.checked = eltExtra.checked = eltErrors.checked = false;
		eltToggle.checked = true;
	}
	else
	if (!eltTodo.checked && !eltMissing.checked && !eltExtra.checked && !eltErrors.checked)
	{
		eltMissing.checked = eltTodo.checked = eltExtra.checked = eltErrors.checked = true;
		eltToggle.checked = false;
	}
	filterTree ();
}

function onLoad ()
{
	var eltMissing = document.getElementById ('missing');
	var eltTodo = document.getElementById ('todo');
	var eltExtra = document.getElementById ('extra');
	var eltErrors = document.getElementById ('errors');
	eltMissing.checked = eltTodo.checked = eltExtra.checked = eltErrors.checked = true;
	filterTree ();
}

if (document.layers)
{
	document.captureEvents (Event.MOUSEUP);
	document.onmouseup = clickHandler;
}
else if (document.attachEvent)
{
	document.attachEvent('onclick', clickHandler);
}
else if (document.addEventListener)
{
	document.addEventListener('click', clickHandler, false);
}
else 
	document.onclick = clickHandler;

