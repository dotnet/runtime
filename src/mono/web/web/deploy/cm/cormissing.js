function toggle (elt)
{
	if (elt == null)
		return;

	var eltLink = elt.firstChild;
	if (eltLink != null && eltLink.className == 'toggle')
	{
		var ich = elt.className.indexOf ('_collapsed');
		if (ich < 0)
		{
			eltLink.src = 'cm/toggle_plus.gif';
			elt.className += '_collapsed';
		}
		else
		{
			eltLink.src = 'cm/toggle_minus.gif';
			elt.className = elt.className.slice (0, ich);
		}
	}
}

function setView (elt, fView)
{
	var eltLink = elt.firstChild;
	if (eltLink != null && eltLink.className == 'toggle')
	{
		var ich = elt.className.indexOf ('_collapsed');
		if (ich < 0 && !fView)
		{
			eltLink.src = 'cm/toggle_plus.gif';
			elt.className += '_collapsed';
		}
		else if (ich >= 0 && fView)
		{
			eltLink.src = 'cm/toggle_minus.gif';
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
			if (image.name == 'status')
			{
				var strImage = trimSrc (rgImages [iImage].src);
				if (dictTypes [strImage])
				{
					fView = true;
					break;
				}
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
	if (eltLink != null && eltLink.className == 'toggle')
	{
		var ich = elt.className.indexOf ('_collapsed');
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
		if (span.className == 'name')
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

	if (elt.className == 'name')
	{
		var strClass;
		var strField;

		elt = getParentDiv (elt);
		if (elt.className == 'method' ||
			elt.className == 'property' ||
			elt.className == 'event' ||
			elt.className == 'constructor' ||
			elt.className == 'field')
		{
			strField = getName (elt).toLowerCase ();
			var match = strField.match ( /[A-Z0-9_]*/i );
			if (match)
				strField = match [0];
			elt = getParentDiv (elt);
			elt = getParentDiv (elt);
		}
		if (elt.className == 'class' ||
			elt.className == 'struct' ||
			elt.className == 'enum')
		{
			strClass = getName (elt).toLowerCase () + 'class';
			elt = getParentDiv (elt);
		}
		else if (elt.className == 'delegate')
		{
			strClass = getName (elt).toLowerCase () + 'eventhandler';
			elt = getParentDiv (elt);
		}
		if (elt.className == 'namespace')
		{
			var strNamespace = getName (elt).toLowerCase ().replace ('.', '');
			if (strClass)
				strNamespace += strClass;
			if (strField)
				strNamespace += strField;
			if (strClass || strField)
				strNamespace += 'topic';

			window.open ('http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpref/html/frlrf' + strNamespace + '.asp', 'MSDN');
			//window.open ('http://msdn.microsoft.com/library/en-us/cpref/html/frlrf'+strNamespace+'.asp', 'MSDN');
		}
	}
/*
	else if (elt.className == 'filter')
	{
		var strType = trimSrc (elt.src);
		var dictTypes = new Object ();
		if (evt.shiftKey || evt.ctrlKey)
		{
			if (evt.ctrlKey)
				strType = '';

			if (strType != 'complete')
				dictTypes ['complete'] = true;
			if (strType != 'missing')
				dictTypes ['missing'] = true;
			if (strType != 'todo')
				dictTypes ['todo'] = true;
		}
		else
		{
			dictTypes [strType] = true;
		}
		viewAll (document.getElementById ('ROOT'), dictTypes);
	}
*/
	else
	{
		if (elt.parentNode && elt.parentNode.className == 'toggle')
			elt = elt.parentNode;
		else if (elt.className != 'toggle')
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
		dictTypes ['missing'] = true;
	if (eltTodo.checked)
		dictTypes ['todo'] = true;
	dictTypes ['completed'] = true;

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

