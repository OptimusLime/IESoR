var setupCount = 0;
var margin, x, fitness, y, color, xAxis, yAxis, width, height, svg;

var setupSVG = function()
{
    margin = {top: 20, right: 20, bottom: 30, left: 40},
        width = 900 - margin.left - margin.right,
        height = 500 - margin.top - margin.bottom;

    x = d3.scale.linear()
        .range([0, width]);

    fitness = d3.scale.linear()
        .range([3, 10]);

    y = d3.scale.linear()
        .range([height, 0]);

    if(dataObjects){
        x.domain(d3.extent(dataObjects, function(d) { return d.x; })).nice();
        y.domain(d3.extent(dataObjects, function(d) { return d.y; })).nice();
    }

    color = d3.scale.category10();

    xAxis = d3.svg.axis()
        .scale(x)
        .orient("bottom")
        .ticks(25);

    yAxis = d3.svg.axis()
        .scale(y)
        .orient("left")
        .ticks(25);

    svg = d3.select("#d3").append("svg")//insert("svg", ":first-child")
        .attr("width", width + margin.left + margin.right)
        .attr("height", height + margin.top + margin.bottom)
        .attr("class", "svgClass" + setupCount)
        .attr("id", "mainSVG" + setupCount++)
        .append("g")
        .attr("transform", "translate(" + margin.left + "," + margin.top + ")");

};

var deleteSVG = function(svgElement)
{
    //delete svgElement;
    //$('#d3').remove('.svg');
    $('#mainSVG' + (setupCount-1)).remove();//append('<div id="pcaViewer"></div>');
    $('#pcaViewer').remove();//append('<div id="pcaViewer"></div>');
};

var doFullPCA = function(websocketClient, errFun, cleanFunction, setupFunction, dataFunction, params)
{
    //this will call deleteSVG
    cleanFunction();
    //this will call setupSVG
    setupFunction();

    //set up d3
    runFullPCA(params.useXCom, params.xBins, params.yBins, params.selector, params.percent,
        dataFunction,
        function(err){ //still need to hide on body error!

            errFun(err);
//            console.log('Error: ' );
//            console.log(err);
        }
    );
};
