

//Returns the JSON version of the XML file loaded in from the input element -- with ID  == elementname
function createXMLFileReader(elementName, jsonCallback)
{
    if (window.File && window.FileReader && window.FileList && window.Blob) {
        // Great success! All the File APIs are supported.

        function handleFileSelect(evt) {
            var files = evt.target.files; // FileList object

            // Loop through the FileList
            for (var i = 0, f; f = files[i]; i++) {

                var reader = new FileReader();

                // Closure to capture the file information.
                reader.onload = (function(theFile) {
                    return function(e) {
                        // Print the contents of the file

                        var parser=new DOMParser();
                        var doc=parser.parseFromString(e.target.result,"text/xml");
                        console.log(doc);

                        var xml = new JKL.ParseXML();
                        var xd =  xml.parseDocument(doc);
                        if(xd)
                            jsonCallback(xd["#document"]);
                        else
                            jsonCallback({status: "fail", reason: "Honestly, no clue"});

                    };
                })(f);

                // Read in the file
                //reader.readAsDataText(f,UTF-8);
                reader.readAsText(f,"UTF-8");
            }
        }

        document.getElementById(elementName).addEventListener('change', handleFileSelect, false);

    }
    else
    {
        console.log("Browser doesn't support FileReader API, need to create an alternative")
    }

}






function importXML(xmlfile)
{
    var xmlDoc;
    try
    {
        var xmlhttp = new XMLHttpRequest();
        xmlhttp.open("GET", xmlfile, false);
    }
    catch (Exception)
    {
        var ie = (typeof window.ActiveXObject != 'undefined');

        if (ie)
        {
            xmlDoc = new ActiveXObject("Microsoft.XMLDOM");
            xmlDoc.async = false;
            while(xmlDoc.readyState != 4) {}
            xmlDoc.load(xmlfile);
            xmlloaded = true;
            return xmlDoc;
        }
        else
        {
            xmlDoc = document.implementation.createDocument("", "", null);
            xmlDoc.load(xmlfile);
            xmlloaded = true;
            return xmlDoc;
        }
    }

    if (!xmlloaded)
    {
        xmlhttp.setRequestHeader('Content-Type', 'text/xml')
        xmlhttp.send("");
        xmlDoc = xmlhttp.responseXML;
        xmlloaded = true;
        return xmlDoc;
    }
}
//this will create and call ensureParserExists()
(function ensureParserExists()
{
    if(typeof(DOMParser) == 'undefined') {
        DOMParser = function() {}
        DOMParser.prototype.parseFromString = function(str, contentType) {
            if(typeof(ActiveXObject) != 'undefined') {
                var xmldata = new ActiveXObject('MSXML.DomDocument');
                xmldata.async = false;
                xmldata.loadXML(str);
                return xmldata;
            } else if(typeof(XMLHttpRequest) != 'undefined') {
                var xmldata = new XMLHttpRequest;
                if(!contentType) {
                    contentType = 'application/xml';
                }
                xmldata.open('GET', 'data:' + contentType + ';charset=utf-8,' + encodeURIComponent(str), false);
                if(xmldata.overrideMimeType) {
                    xmldata.overrideMimeType(contentType);
                }
                xmldata.send(null);
                return xmldata.responseXML;
            }
        }
    }
})();