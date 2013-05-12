
//setup text saving
var text = $("text")
    , text_options_form = $("#text-options")
    , text_filename = $("#text-filename")
    , get_blob = function() {
        return window.Blob;
    };


var defaultSaveFunction = function()
{
    if(!$.isEmptyObject(cachedObjects))
    {
        console.log('Saving!');
//            console.log(JSON.stringify(cachedObjects));
        var BB = get_blob();
        saveAs(
            new BB(
                [JSON.stringify(cachedObjects)]
                , {type: "text/json;charset=" + document.characterSet}
            )
            , text_filename.val() +  ".json"
        );

        var BB = get_blob();
        saveAs(
            new BB(
                [JSON.stringify(dataObjects)]
                , {type: "text/json;charset=" + document.characterSet}
            )
            , "pca" + text_filename.val() +  ".json"
        );

    }
};

var setupPCASave = function(textName, textOptionsName, textFNName, saveFunction)
{
    text = $(textName);
    text_options_form = $(textOptionsName);
    text_filename = $(textFNName);

    var text_save_function;
    if(typeof saveFunction === 'function')
        text_save_function = saveFunction;
    else
        text_save_function = defaultSaveFunction;

    //    $('#save-button')
    text_options_form.submit(function(event) {
        event.preventDefault();
        text_save_function();
    });//, false);

};

var saveSVGToFile = function(data, fileName)
{
    var BB = get_blob();
    saveAs(
        new BB(
            [data]
            , {type: "text/plain;charset=" + document.characterSet}
        )
        , fileName +  ".svg"
    );
};

//private PCA
var sanitizePCA = function()
{
    $('#d3').append('<div id="temporary"></div>');


    if(!$.isEmptyObject(cachedObjects)){

        var replaceData = [];

        for(var i=0; i< dataObjects.length; i++)
        {

            var data  = dataObjects[i];

            var uid = data.uid;
            //now we want to set up our genome inside viewer below the PCA Chart (or to the side)
            var sizedWorld = addGenomeToSizedDiv(cachedObjects[uid], {containID: '#temporary', width: 400, height: 400, zombieMode: true});

            var behavior = sizedWorld.runSimulationForBehavior( {startEval: true, visual: true,
                isVisible: true,
                drawBehavior: false,
                zombieMode: true, genomeID:uid});

            //fix the fitness and move one!
            data.absoluteFitness = behavior.behavior.fitness;

            replaceData.push(data);
        }

        dataObjects = replaceData;
        console.log('Replaced data objects!')

        var prevFetch = shouldFetchCache;
        shouldFetchCache = false;

        deleteSVG();
        $('#mainSVG' +(setupCount -1)).remove();
        setupSVG();
        enterDataD3(replaceData);

        shouldFetchCache = prevFetch;

    }

    $('#temporary').remove();

}