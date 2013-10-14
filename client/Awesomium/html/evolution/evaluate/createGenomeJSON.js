var fs = require('fs');
var util = require('util');
var xml2js = require('xml2js');
var path = require('path');

var genomeSharpToJS = require('../interpret/genomeCSharpToJS.js');

var cppnjs = require('cppn');
var neatjs = require('neatjs');

//we want to load up a file, process the objects, then save out a new json file

var attrkey = '$';
var parser = new xml2js.Parser({explicitArray : false, attrkey: attrkey, mergeAttrs: true});


var allGenomes = {};
var totalGenomeParsed = 0;
parser.addListener('end', function(genomeLoaded) {

    var genome = genomeLoaded['genome'];

    genome['behavior']['list'] = "";
    var gid = genome['id'];

    allGenomes[gid] = genome;

    totalGenomeParsed--;

    if(totalGenomeParsed <= 0)
    {
        //save to file!
        fs.writeFile(path.resolve(genomesPath, './allSelectedGenomeXML.json'), JSON.stringify(allGenomes), function(err) {
            if(err) {
                console.log(err);
            } else {
                console.log("The file was saved!");
            }
        });
    }
});

var genomesPath = path.resolve(__dirname, '../../data/genomes');
fs.readFile(path.resolve(genomesPath, './selectedGenomeIDs.json'), 'utf8', function (err,data) {
    if (err) {
        console.log(err);
        throw err;
    }
    var jData = JSON.parse(data);
    //we should have json object here, with list for genomes
    for(var gid in jData)
    {
        console.log("Gid: " + gid);


        //gotta fetch the xml object now
        fs.readFile(path.resolve(genomesPath, './genome_' + gid + '.xml'), 'utf8', function(errXml, xml)
        {
            if(errXml)
            {
                console.log(errXml);
                throw errXml;
            }
            totalGenomeParsed++;
           parser.parseString(xml);
        });

    }


    //we need to parse the data, and create some genomes!
//    parser.parseString(data);//, function (err, result) {



});