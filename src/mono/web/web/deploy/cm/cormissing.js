function toggle (elt)
{
	if (elt == null)
		return;

	var eltLink = elt.firstChild;
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
	var eltLink = elt.firstChild;
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

function trimSrc (strSrc)
{
	return strSrc.slice (strSrc.lastIndexOf ('/') + 1, strSrc.lastIndexOf ('.'));
}

function getChildrenByTagName (elt, strTag)
{
	strTag = strTag.toLowerCase ();
	var rgChildren = new Array ();
	var eltChild = elt.firstChild;
	while (eltChild)
	{
		if (eltChild.tagName && eltChild.tagName.toLowerCase () == strTag)
			rgChildren.push (eltChild);
		eltChild = eltChild.nextSibling;
	}
	return rgChildren;
}

function viewAll (elt, dictTypes)
{
	var fView = false;
	var rgElts = getChildrenByTagName (elt, 'DIV');
	var cElts = rgElts.length;
	if (cElts == 0)
	{
		var rgImages = getChildrenByTagName (elt, 'IMG');
		var cImages = rgImages.length;
		for (var iImage = 0; iImage < cImages; iImage++)
		{
			var image = rgImages [iImage];
			var strImage = trimSrc (rgImages [iImage].src);
			if (dictTypes [strImage])
			{
				fView = true;
				break;
			}
		}
	}
	else
	{
		var iElt;
		for (iElt = 0; iElt < cElts; iElt ++)
			fView |= viewAll (rgElts [iElt], dictTypes);
	}
	elt.style.display = fView ? '' : 'none';
	return fView;
}

function getView (elt)
{
	var eltLink = elt.firstChild;
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

		elt = getParentDiv (elt);
		if (elt.className == 'x')	// constructor
		{
			strField = 'ctor';
			elt = getParentDiv (elt);
		}
		else
		if (elt.className == 'm' ||	// method
			elt.className == 'p' ||	// property
			elt.className == 'e' ||	// event
			elt.className == 'f')	// field
		{
			strField = getName (elt).toLowerCase ();
			var match = strField.match ( /[\.A-Z0-9_]*/i );
			if (match)
				strField = match [0];
			elt = getParentDiv (elt);

		}

		if (elt.className == 'c' ||	// class
			elt.className == 's' ||	// struct
			elt.className == 'd' ||	// delegate
			elt.className == 'en')	// enum
		{
			strClass = getName (elt).toLowerCase () + 'class';
			if (elt.className == 'en')
				strField = null;
			elt = getParentDiv (elt);
		}

		if (elt.className == 'n')	// namespace
		{
			var re = /\./g ;
			var strNamespace = getName (elt).toLowerCase ().replace (re, '');
			if (strClass)
				strNamespace += strClass;
			if (strField)
				strNamespace += strField;
			if (strClass || strField)
				strNamespace += 'topic';

			window.open ('http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpref/html/frlrf' + strNamespace + '.asp', 'MSDN');
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

	var dictTypes = new Object ();
	if (eltMissing.checked)
		dictTypes ['sm'] = true;
	if (eltTodo.checked)
		dictTypes ['st'] = true;
	dictTypes ['sc'] = true;

	viewAll (document.getElementById ('ROOT'), dictTypes);
}

function selectMissing ()
{
	var eltMissing = document.getElementById ('missing');
	var eltTodo = document.getElementById ('todo');
	if (!eltTodo.checked && !eltMissing.checked)
		eltTodo.checked = true;
	filterTree ();
}

function selectTodo ()
{
	var eltMissing = document.getElementById ('missing');
	var eltTodo = document.getElementById ('todo');
	if (!eltTodo.checked && !eltMissing.checked)
		eltMissing.checked = true;
	filterTree ();
}

function onLoad ()
{
	var eltMissing = document.getElementById ('missing');
	var eltTodo = document.getElementById ('todo');
	eltMissing.checked = true;
	eltTodo.checked = true;
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

