//this is a function to grab multiple bodies from our server
function getGenome(genomeID, bodyCallback, errorCallback)
{
    console.log('Sending single genome request');

    $.ajax({
        url: "http://127.0.0.1:3000/genome/" + genomeID,
        type: 'GET',
        cache: false,
        timeout: 30000,
        complete: function() {
            //called when complete
            console.log('done with get bodies request');
        },

        success: function(data) {
            console.log('Single genome success');
            bodyCallback(data);

        },

        error: function(err) {
            console.log('Single genome error: ' + err.responseText);
            if(errorCallback)
                errorCallback(err);
        }
    });
};

function getNodes(nodes, bodyCallback, errorCallback)
{
    console.log('Sending multi-node request');

    $.ajax({
        url: "http://127.0.0.1:3000/node/batch",
        type: 'GET',
        data: {"nodes": nodes},
        cache: false,
        timeout: 30000,
        complete: function() {
            //called when complete
            console.log('done with get nodes request');
        },

        success: function(data) {
            console.log('Multi node success');
            bodyCallback(data);

        },

        error: function(err) {
            console.log('Multi node error: ' + err.responseText);
            if(errorCallback)
                errorCallback(err);
        }
    });
};

function getHilbertCurve(bodyCallback, errorCallback)
{
    console.log('Sending hilbert request');

    $.ajax({
        url: "http://127.0.0.1:3000/hilbert",
        type: 'GET',
        cache: false,
        timeout: 30000,
        complete: function() {
            //called when complete
            console.log('done with get hilbert request');
        },

        success: function(data) {
            console.log('hilbert success');
            bodyCallback(data);

        },

        error: function(err) {
            console.log('hilbert error: ' + err.responseText);
            if(errorCallback)
                errorCallback(err);
        }
    });
};