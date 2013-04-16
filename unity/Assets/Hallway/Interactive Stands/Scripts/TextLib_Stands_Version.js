#pragma strict

//---------------------------------------------------------------------------------------
//Условия:
//1) Cкрипт должен быть прикреплен к plane, на который надо выводить текст.
//2) Plane должен быть повернут следующим образом: x = 270, y = 0, z = 0.
//3) Родителем plane'а должен быть пустой объект, и вот уже его вертите как хотите.
//4) Scale plane'а нужно менять непосредственно у него, scale родителя должен быть 1-1-1

//Display(hdr, txt, 0.06, 0.04, 0.8, 7, 10, ">", -0.01); - вывести текст txt с заголовком hdr,
//с размером заголовка 0.06,
//с размером основного текста 0.04,
//с расстоянием между строчками 0.8,
//с отступом для красной строки в 7 пробелов,
//с отступом для каждого элемента списка в 10 пробелов,
//с символом списка ">",
//с отступом текста по оси Z от родителя (не от plane'а) в 0.01
	
//Undisplay(); - удалить все объекты, которые насоздавал Display

//обе функции предполагается вызывать извне, сам файл ничего не делает
//---------------------------------------------------------------------------------------

//три ссылки, куда нужно затащить соответствующие ассеты через инспектор
var TextFont : Font; //шрифт
var TextMaterial_Main: Material; //материал основного текста
var TextMaterial_Other: Material; //материал заголовка, числа страниц и нижних кнопок

//переменные, вынесенные за пределы функций, поскольку все (ну, почти все, остальное для
//единообразия) будут использоваться в двух вышеупомянутых скриптах
var current_page : int;
var pages_number : float;
var p = new Array();
var t : GameObject;
var h : GameObject;
var c : GameObject;
var prev : GameObject;
var next : GameObject;

//учет просмотренных страниц
var have_seen = new Array();

//создание текста-объекта (программный аналог GameObject -> CreateOther -> 3D Text)
//под названием name с координатами pos и со всеми прочими параметрами,
//которые передаются сверху
function createText(name:String, pos:Vector3, offZ:float, size:float, spacing:float,
					an:TextAnchor, al:TextAlignment, fnt:Font, mat:Material) {
	var obj : GameObject;
	obj = new GameObject(name);
	obj.AddComponent(TextMesh);
	obj.AddComponent(MeshRenderer);
	obj.transform.parent = transform.parent.transform;
	obj.transform.localPosition = pos;
	obj.transform.localRotation = Quaternion.identity;
	obj.GetComponent(TextMesh).offsetZ = offZ;
	obj.GetComponent(TextMesh).characterSize = size;
	obj.GetComponent(TextMesh).lineSpacing = spacing;
	obj.GetComponent(TextMesh).anchor = an;
	obj.GetComponent(TextMesh).alignment = al;
	obj.GetComponent(TextMesh).font = fnt;
	obj.GetComponent(MeshRenderer).material = mat;
	return obj;
}

//собственно алгоритм выравнивания по ширине
//на вход получает абзац текста единой строкой, на выход дает абзац текста так же единой строкой
//с добавленными переносами "\n" и нужным количеством пробелов после каждого слова
function justifyParagraph(line:String, width:float, indent:int, list_indent:int, list_mark:String, t:GameObject) {
		
	var s = line.Split(" "[0]); //превращаем входную строку в массив слов, разбивая ее по пробелам
	//знаки препинания становятся частью последнего слова (например, "конец.")
	var i = 0; var j = 0; var k = 0;
	var first = 0; var last = 0;
	var spaces = ""; var list = false;
	
	//входная строка может быть либо обычным абзацем, либо элементом списка
	//в обычном абзаце отступ есть только в первой (красной) строке
	//в списке отступ одинаков для каждой строчки, плюс перед первой строкой пишется элемент списка,
	//будь то точка или галочка немаркированного или очередная цифра маркированного списков
	//эта точка/галочка/цифра передана нам в параметре list_mark
	//в начале каждого элемента списка должен следовать служебный символ "\a", так мы их отличаем
	
	//отступ абзаца - это параметр indent, отступ списка - list_indent, оба выражены в количестве пробелов
	//для абзаца мы просто превращаем первое слово из, например, "Первое" в "     Первое"
	//для списка мы еще втискиваем символ списка
	if (s[0][0]=="\a") {
		list = true; spaces = "";
		for (j=0; j<list_indent-list_mark.length-2; j++) spaces += " ";
		spaces += list_mark + " ";
		s[0] = spaces + s[0];
	} else {
		list = false; spaces = "";
		for (j=0; j<indent; j++) spaces += " ";
		s[0] = spaces + s[0];
	}	
	
	//цикл по словам
	//выводим на стенд все слова, начиная с первого в строке и заканчивая текущим,
	//а потом смотрим, не превысила ли еще строка необходимой ширины
	while (i<s.length-1) {
		t.GetComponent(TextMesh).text = "";
		for (j=first; j<=i; j++) t.GetComponent(TextMesh).text += s[j];
		//print(t.GetComponent(TextMesh).text);
		t.GetComponent(TextMesh).text += " " + s[i+1];
		if (t.GetComponent(MeshRenderer).bounds.size.x <= width) {
			s[i] += " "; //если нет, то добавляем к слову один пробел и идем дальше
		} else {
			s[i] += "\n"; //если да, то добавляем к слову перенос "\n"
			last = i-1;
			
			//и начинаем выравнивать законченную строку по ширине
			//first - это индекс первого слова в строке, last - предпоследнего
			//сейчас в конце каждого слова от first до last есть по 1 пробелу
			//мы начинаем по очереди добавлять еще (сначала у первого слова будет 2 пробела,
			//потом у первого и второго 2 пробела, потом у всех 2 пробела, потом у первого
			//3 пробела и т.д., пока не превысим ширину)
			if (last>=first) { //если, конечно, в строке хотя бы 2 слова, иначе выравнивать нечего
				k = first;
				do {
					var v = s[k];
					s[k] += " ";
					k++;
					if (k>last) k = first;
					t.GetComponent(TextMesh).text = "";
					for (j=first; j<=i; j++) t.GetComponent(TextMesh).text += s[j];
					//print(t.GetComponent(TextMesh).text);
				}
				while (t.GetComponent(MeshRenderer).bounds.size.x <= width);
				//как только ширина превышена, откатываем последнее изменение и сворачиваемся
				if (k==first) k=last; else k--;
				s[k] = v;
			}	
																
			first = i+1; //переходим на следующую строку, индекс первого слова становится другим
			if (list) { 
				//если работаем со списком, то этому первому слову нужно сразу же
				//подобавлять пробелом, как мы делали это с первой строчкой
				spaces = "";
				for (j=0; j<list_indent; j++) spaces += " ";
				s[first] = spaces + s[first];
			}
		}
		i++;
	}
	
	//выходную строку склеиваем из все того же массива, только теперь ко всем последним словам
	//строки добавлен символ "\n", а ко всем остальным словам - необходимое для выравнивания
	//количество пробелов
	t.GetComponent(TextMesh).text = "";
	var res = "";
	for (j=0; j<s.length; j++) res += s[j];
	return res;
}

//выравнивание по ширине всего текста
//на вход получает текст единой строкой, где абзацы разделены символами \r
//разбивает строку на массив абзацев, в цикле обрабатывает каждый из них функцией
//JustifyParagraph, снова сводит воедино и отдает на выход
function justifyText(txt:String, width:float, indent:int, list_indent:int, list_mark:String, t:GameObject) {
	var txt_formatted = "";
	var s = txt.Split("\r"[0]);
	for (var i=0; i<s.length; i++) { 
		s[i] = justifyParagraph(s[i], width, indent, list_indent, list_mark, t);
		if (i!=s.length-1) s[i] += "\n"; //предыдущая функция не добавляет переноса к последнему слову абзаца,
		//поэтому нужно приписать ее тут, если только это не самый последний абзац текста
	}	
	for (i=0; i<s.length; i++) txt_formatted += s[i];
	return txt_formatted;
}

//разбиение отформатированного текста на нужное количество страниц
//в зависимости от высоты стенда (height)
//на выход получаем отформатированный текст-строку от justifyText,
//на выход дает массив страниц p (каждый элемент - текст страницы единой строкой)
function divideText(txt_formatted:String, height:float, t:GameObject) {
	//определяем высоту одной строки, а через нее - сколько строк уместится на одной странице
	t.GetComponent(TextMesh).text = "1";
	var lines_per_page : float;
	lines_per_page = Mathf.FloorToInt(height/t.GetComponent(MeshRenderer).bounds.size.y);
	
	var l = txt_formatted.Split("\n"[0]); //дробим отформатированный текст в массив строк	
	pages_number = Mathf.CeilToInt(l.length/lines_per_page); //считаем число страниц
	//print(l.length); print(lines_per_page); print(l.length/lines_per_page); print(pages_number);
	var j: int;
	//раскидываем строки по страницам (от первой до предпоследней)
	for (var i: int = 0; i<=pages_number-2; i++) {
		p[i] = "";
		for (j=i*lines_per_page; j<=(i+1)*lines_per_page-2; j++) p[i]+=l[j]+"\n";
		p[i] += l[(i+1)*lines_per_page-1];
	}
	//набираем строки в последнюю страницу (здесь цикл немного другой, поэтому отдельно)
	p[pages_number-1] = "";
	for (j=(pages_number-1)*lines_per_page; j<=l.Length-2; j++) p[pages_number-1]+=l[j]+"\n";
	p[pages_number-1] += l[l.Length-1];
	
	return p;
}

//итоговая функция, создает все нужные объекты и выводит текст на стенд
function Display(hdr:String, txt:String, hdr_size:float, txt_size:float, spacing:float,
				 indent:int, list_indent:int, list_mark:String, offZ: float, withPic:boolean) {
	var pos : Vector3;
	var center = transform.localPosition;
	var width = GetComponent(MeshFilter).mesh.bounds.size.x * transform.localScale.x;
	var height = GetComponent(MeshFilter).mesh.bounds.size.z * transform.localScale.z;
	
	/*---небольшое пояснение касательно того, как получаются width и height---
	в bounds хранится информация о центре (что, в принципе, можно узнать и через transform.position)
	и размерах (а вот это уже есть только здесь) объекта. Этот bounds есть как в renderer, так и в mesh;
	в первом размеры глобальные, во втором - локальные, причем не просто локальные, но еще и без учета
	масштабирования (у plane, то есть, всегда будет дефолтное 10 на 10), поэтому домножаем*/

	/*с поворотами (которые влияют плейна на ширину по x, которая используется внутри функций)
	справляемся так: запоминаем поворот родителя плейна, потом сбрасываем его в ноль, размещаем все,
	что нужно, а потом поворачиваем родителя (то есть, всю группу, уже вместе с текстом) в исходное
	состояние*/
	var pt = this.transform.parent.transform;
	var group_rotation = pt.rotation;
	pt.rotation = Quaternion.identity;
																																																																																																																																																																																																																															
	pos = Vector3(center.x, center.y+height/2, 0);
	h = createText("Header", pos, offZ, hdr_size, spacing, TextAnchor.UpperCenter, 
					TextAlignment.Center, TextFont, TextMaterial_Other);
	h.GetComponent(TextMesh).text = hdr;
			
	pos = Vector3(center.x, center.y-height/2+0.1, 0);
	c = createText("PageCounter", pos, offZ, txt_size, spacing, TextAnchor.LowerCenter,
					TextAlignment.Center, TextFont, TextMaterial_Other);
	c.GetComponent(TextMesh).text = "Стр.1 из 1";
	
	pos = Vector3(center.x-width/2+0.1, center.y-height/2+0.1, 0);
	prev = createText("PrevButton", pos, offZ, txt_size, spacing, TextAnchor.LowerLeft,
					   TextAlignment.Left, TextFont, TextMaterial_Other);
	prev.GetComponent(TextMesh).text = "<<< Назад";
	
	pos = Vector3(center.x+width/2-0.1, center.y-height/2+0.1, 0);
	next = createText("NextButton", pos, offZ, txt_size, spacing, TextAnchor.LowerRight,
					   TextAlignment.Right, TextFont, TextMaterial_Other);
	next.GetComponent(TextMesh).text = "Далее >>>";

	pos = Vector3(center.x-width/2+0.1, center.y+height/2-h.renderer.bounds.size.y-0.05, 0);
	t = createText("Content", pos, offZ, txt_size, spacing, TextAnchor.UpperLeft,
					TextAlignment.Left, TextFont, TextMaterial_Main);
	
	var txt_formatted = justifyText(txt, width-0.2, indent, list_indent, list_mark, t);
	p = divideText(txt_formatted, height - h.renderer.bounds.size.y - c.renderer.bounds.size.y - 0.2, t);
	
	//здесь и в соседних скриптах - withPic=true означает, что это стенд НИРС, где, помимо текста,
	//еще есть последняя страница с картинкой, которую мы учитываем
	if (withPic) pages_number++;
	
	current_page = 1;
	t.GetComponent(TextMesh).text = p[current_page-1];
	c.GetComponent(TextMesh).text = "Стр."+current_page+" из "+pages_number;
	
	//BoxCollider'ы, чтобы по тексту можно было кликать
	prev.AddComponent(BoxCollider); next.AddComponent(BoxCollider);
	
	if (!withPic) {
		//выуживаем из ассетов заготовленные скрипты
		prev.AddComponent("TextLib_PrevPage_Stands_Version"); next.AddComponent("TextLib_NextPage_Stands_Version");
		//передаем им ссылку на стенд
		prev.GetComponent.<TextLib_PrevPage_Stands_Version>().background = transform.gameObject;
		next.GetComponent.<TextLib_NextPage_Stands_Version>().background = transform.gameObject;
	} else {	
		prev.AddComponent("WP_TextLib_PrevPage"); next.AddComponent("WP_TextLib_NextPage");
		prev.GetComponent.<WP_TextLib_PrevPage>().background = transform.gameObject;
		next.GetComponent.<WP_TextLib_NextPage>().background = transform.gameObject;
		prev.GetComponent.<WP_TextLib_PrevPage>().pic = transform.parent.transform.Find("Pic").gameObject;
		next.GetComponent.<WP_TextLib_NextPage>().pic = transform.parent.transform.Find("Pic").gameObject;
	}
	
	pt.rotation = group_rotation;
	
	have_seen[0] = true;
	for (var kk=1; kk<pages_number; kk++) have_seen[kk] = false;		
}

function CheckIfComplete() {
	have_seen[current_page-1] = true;
 	var i = 0;
 	while ((i < pages_number) && (have_seen[i])) i++;
 	if (i == pages_number) GetComponent.<CameraChange>().addInfo = "completed";
}

function Undisplay() {
	Destroy(h); Destroy(c);
	Destroy(prev); Destroy(next);
	Destroy(t);
}