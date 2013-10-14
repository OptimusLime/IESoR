var timeoutMS = 1200000;
//this is a function to grab a single body from our server
function getBody(bodyCallback)
{
    console.log('Senidng body requests');
    $.getJSON('http://127.0.0.1:3000/get',function(jsonData)
    {
        console.log('Return body request');
        bodyCallback(jsonData);
    });
};

//this is a function to grab multiple bodies from our server
function getBodies(numberBodies, bodyCallback, errorCallback)
{
    console.log('Sending multibody request');

    $.ajax({
        url: "http://127.0.0.1:3000/getBodies",
        type: 'GET',
        data: {"numberOfBodies" : numberBodies },
        cache: false,
        timeout: timeoutMS,
        complete: function() {
            //called when complete
            console.log('done with get bodies request');
        },

        success: function(data) {

            console.log('Multibody success');
            bodyCallback(data);

        },

        error: function(err) {
            console.log('Multibody error: ' + err.responseText);
            if(errorCallback)
                errorCallback(err);
        }
    });
};
function getGenomes(genomeIDArray, bodyCallback, errorCallback)
{
    console.log('Sending multi-genomeID request');

    $.ajax({
        url: "http://127.0.0.1:3000/getGenomes",
        type: 'GET',
        data: {"genomeIDArray" : genomeIDArray },
        cache: false,
        timeout: timeoutMS,
        complete: function() {
            //called when complete
            console.log('done with get genomeIDs request');
        },

        success: function(data) {

            console.log('Multi-genome success');
            bodyCallback(data);

        },

        error: function(err) {
            console.log('Multibody error: ' + err.responseText);
            if(errorCallback)
                errorCallback(err);
        }
    });

};
function getGenomeEvaluations(genomeIDArray, bodyCallback, errorCallback)
{
    console.log('Sending multi-genomeID eval request');

    $.ajax({
        url: "http://127.0.0.1:3000/zombieEvaluation",
        type: 'GET',
        data: {"genomeIDArray" : genomeIDArray },
        cache: false,
        timeout: timeoutMS,
        complete: function() {
            //called when complete
            console.log('done with get genomeIDs request');
        },

        success: function(data) {

            console.log('Multi-genome eval success');
            bodyCallback(data);

        },

        error: function(err) {
            console.log('Multibody eval error: ' + err.responseText);
            if(errorCallback)
                errorCallback(err);
        }
    });

};

function getCurrentGeneration(bodyCallback, errorCallback)
{
    console.log('Sending Latest Generation request');

    $.ajax({
        url: "http://127.0.0.1:3000/getCurrentGeneration",
        type: 'GET',
        cache: false,
        timeout: timeoutMS,
        complete: function() {
            //called when complete
            console.log('done with Generation request');
        },

        success: function(generationIDs) {
            console.log('Archive returned');
            bodyCallback(generationIDs);

        },

        error: function(err) {
            console.log('Generation error: ' + err.responseText);
            if(errorCallback)
                errorCallback(err);
        }
    });
}
function getBestIndividuals(uid, bodyCallback, errorCallback)
{
    console.log('Sending Best Individual request');

    $.ajax({
        url: "http://127.0.0.1:3000/getBestBodies?uid="+ uid,
        type: 'GET',
        cache: false,
        timeout:timeoutMS ,
        complete: function() {
            //called when complete
            console.log('done with best request');
        },

        success: function(bestBodies) {
            console.log('best returned');
            bodyCallback(bestBodies);

        },

        error: function(err) {
            console.log('Generation error: ' + err.responseText);
            if(errorCallback)
                errorCallback(err);
        }
    });
}
function runFullPCA(firstBehavior, xBin, yBin, selector, percent, bodyCallback, errorCallback)
{
    //request pca with a certain percentage
    ajaxWIN('runFullPCA?firstBehavior=' + firstBehavior + '&xBins=' + xBin + '&yBins=' + yBin  + '&selector=' + selector + '&percent=' + percent, bodyCallback, errorCallback);
}

function runPCA(bodyCallback, errorCallback)
{
    ajaxWIN('runPCA',bodyCallback, errorCallback);
}
function ajaxWIN(route, bodyCallback, errorCallback)
{
    console.log('Sending Latest PCA request');

    $.ajax({
        url: "http://127.0.0.1:3000/" + route,
        type: 'GET',
        cache: false,
        timeout: timeoutMS,
        complete: function() {
            //called when complete
            console.log('done with PCA request');
        },

        success: function(pcaResults) {
            console.log('PCA Results returned');
            bodyCallback(pcaResults);

        },

        error: function(err) {
            console.log('PCA error: ' + err.responseText);
            if(errorCallback)
                errorCallback(err);
        }
    });

};

function getLatestArchive(bodyCallback, errorCallback)
{
    console.log('Sending Latest Archive request');

    $.ajax({
        url: "http://127.0.0.1:3000/getArchive",
        type: 'GET',
        cache: false,
        timeout: timeoutMS,
        complete: function() {
            //called when complete
            console.log('done with Archive request');
        },

        success: function(archiveIDs) {
            console.log('Archive returned');
            bodyCallback(archiveIDs);

        },

        error: function(err) {
            console.log('Archive error: ' + err.responseText);
            if(errorCallback)
                errorCallback(err);
        }
    });

};


function toggleSelectedBody(genomeID, successCallback, errorCallback)
{

    $.ajax({
        url: "http://127.0.0.1:3000/toggle",
        type: "POST",
        dataType: "json",
        data: JSON.stringify({"genomeID" : genomeID }),
        contentType: "application/json",
        cache: false,
        timeout: timeoutMS,
        complete: function() {
            //called when complete
            console.log('toggle process complete');
        },

        success: function(data) {
            //simply call our success function
           successCallback(data);

        },

        error: function(err) {

            if(errorCallback)
                errorCallback(err);
            else
               console.log('Toggle error: ' + err.responseText);

        }
    });
}