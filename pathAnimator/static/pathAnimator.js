// Dictionaries for player & rolling stock data
var _playerData = {}; // Time: [X, Y, Z, ROT]
var _carData = {};    // CID_TYP: {Time: [X, Y, Z, ROT, SPD]}
const _idSep = ' ';   //    ^ This symbol here

const _worldDimentions = [16360, 16360];

var _traces = [];
var _frames = [];
var _frame = 0;
var _numFrames = 0;
var _fps = 24;
var _animating = false;

const _playerDataIndices = [
    'X',
    'Y',
    'Z',
    'ROT'
];

const _carDataIndices = [
    'CID',
    'TYP',
    'X',
    'Y',
    'Z',
    'ROT',
    'SPD'
];

const _colours = {
    'Player':  '#1f77b4',
    'Caboose': '#d62728',
    'DE2':     '#ff7f0e',
    'DE6':     '#8c564b',
    'SH282':   '#111111',
    'Mod':     '#2ca02c'
};

$(document).ready(function() {
    // Allow file to be drag&dropped to load
    let dragNDrop = $('#pathAnimator');
    dragNDrop.on('dragover', function(e) {
        e.preventDefault();
        e.stopPropagation();
    });
    dragNDrop.on('dragenter', function(e) {
        e.preventDefault();
        e.stopPropagation();
    });
    dragNDrop.on('drop', function(e) {
        e.preventDefault();
        e.stopPropagation();
        $('#filePicker').val('');
        loadFile(e.originalEvent.dataTransfer.files[0]);
    });

    // Allow user-selected file
    $('#filePicker').change(function() {
        if (this.files && this.files[0]) {
            loadFile(this.files[0]);
        }
    });

    // Build the Key/Legend
    for (let i in Object.keys(_colours)) {
        let item = Object.keys(_colours)[i];
        let element =   '<div class="row py-1">' +
                            `<div class="col-2 legendSpot" style="background-color:${_colours[item]}"></div>` +
                            `<div class="col-10">${item}</div>` +
                        '</div>';
        $('#legend').append(element);
    }

    // Create FPS slider
    let fpsSlider = document.getElementById('fpsSlider');
    noUiSlider.create(fpsSlider, {
        start: [_fps],
        tooltips: [wNumb({decimals: 0})],
        step: 1,
        range: {
            'min': [1],
            'max': [60]
        },
        format: wNumb({
            decimals: 0
        })
    });
    fpsSlider.noUiSlider.on('change', function() {
        _fps = fpsSlider.noUiSlider.get();
    });

    $('#plotButton').click(plotData);
    $('#timelapseButton').click(animateData);
});

////////////////////////////////////////////////////////////////////////////////
// Functions for loading & interpreting the CSV file //
////////////////////////////////////////////////////////////////////////////////

/**
 * Reads & interprets the given file
 */
function loadFile(file) {
    if (file.type != 'text/csv') {
        if (file.type.match(/image.*/)) {
            importBackgroundImage(file);
        } else {
            console.warn(`File must be csv, got ${file.type}`)
            alert('File must be .csv!');
        }
        return;
    }

    console.log(`Reading file ${file.name}`)

    let reader = new FileReader();
    reader.onload = function() {
        interpretFile(this.result);
    }
    reader.readAsText(file);
}

/**
 * Interprets the contents of a file
 */
function interpretFile(fileString) {
    console.log('Interpreting file...')
    _playerData = {};
    _carData = {};
    let lines = fileString.split('\n');
    console.log(`Reading ${lines.length} lines...`);
    for (let i=0; i < lines.length; i++) {
        let elements = lines[i].trim().split(',');

        let time = parseInt(elements.splice(0, 1));
        
        // Deal with the player data
        interpretPlayerLine(time, elements.splice(0, _playerDataIndices.length));

        // Deal with the car(s) data
        while (elements.length > 0) {
            interpretCarLine(time, elements.splice(0, _carDataIndices.length));
        }
    }
    _traces = getPlottableTraces();
    _frames = getPlottableFrames();
    console.log('Done');
    plotData();
}

/**
 * Interprets a substring of data relevant to info about the player
 */
function interpretPlayerLine(time, line) {
    if (line.length < 4 || line[0] == '' || line[0] == 'PPosX') {
        // 4 is the minimum complete number of columns there will ever be for this
        return
    }
    _playerData[time] = [
        parseFloat(line[0]),
        parseFloat(line[1]),
        parseFloat(line[2]),
        parseFloat(line[3])
    ];
}

/**
 * Interprets a substring of data relevant to info about one piece of rolling stock
 */
function interpretCarLine(time, line) {
    if (line.length < 6 || line[0] == '' ||  line[0] == 'CID') {
        // 6 is the minimum complete number of columns there will ever be for this
        return;
    }
    let idType = line[0] + _idSep + line[1];
    let result = [
        parseFloat(line[2]),
        parseFloat(line[3]),
        parseFloat(line[4]),
        parseFloat(line[5]),
        parseFloat(line[6])
    ];
    if (!_carData[idType]) {
        _carData[idType] = {};
    }
    _carData[idType][time] = result;
}

////////////////////////////////////////////////////////////////////////////////
// End // Functions for loading & interpreting the CSV file //
////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
// Functions for graphing & animating the data //
////////////////////////////////////////////////////////////////////////////////

/**
 * Returns a list of all plottable traces, grouped by object
 */
function getPlottableTraces() {
    let dataPlot = [];
    let orderedPlayerKeys = Object.keys(_playerData).sort((a, b) => a - b);
    dataPlot.push({
        x: [], y: [],
        name: 'Player',
        type: 'scatter',
        mode: 'lines',
        line: {
            width: 3,
            color: _colours['Player']
        }
    });
    for (let i=0; i < orderedPlayerKeys.length; i++) {
        let key = orderedPlayerKeys[i];
        last(dataPlot).x.push(_playerData[key][0]); // X
        last(dataPlot).y.push(_playerData[key][2]); // Z
    }

    for (let carID in _carData) {
        let car = _carData[carID];
        let orderedCarKeys = Object.keys(car).sort((a, b) => a - b);
        let carType = carID.split(_idSep)[1];
        dataPlot.push({
            x: [], y: [],
            name: carID,
            type: 'scatter',
            mode: 'lines+markers',
            line: {
                width: 3,
                color: _colours[carType] ? _colours[carType] : _colours['Mod']
            },
            marker: {
                size: 3,
                color: _colours[carType] ? _colours[carType] : _colours['Mod']
            }
        });
        for (let i=0; i < orderedCarKeys.length; i++) {
            let key = orderedCarKeys[i];
            last(dataPlot).x.push(car[key][0]); // X
            last(dataPlot).y.push(car[key][2]); // Z
        }
    }

    return dataPlot;
}

/**
 * Plot all traces on a graph
 */
function plotData() {
    _animating = false;
    $('#plotButton').attr('disabled', true);
    $('#timelapseButton').attr('disabled', false);
    $('#animationPlot').empty()
    $('#fpsOption').addClass('d-none');

    console.log('Plotting data');

    let layout = {
        showlegend: true,
        xaxis: {
            range: [0, _worldDimentions[0]],
            visible: false,
            showgrid: false
        },
        yaxis: {
            range: [0, _worldDimentions[1]],
            scaleanchor: 'x',
            visible: false,
            showgrid: false
        },
        dragmode: 'pan',
    };

    Plotly.newPlot( 'animationPlot', _traces, layout, { scrollZoom: true } );
    
    if ($('#animationPlot').attr('src')) {
        console.log("Adding existing background image");
        addBackgroundImage();
    }
}

/**
 * Returns a list of all plottable objects, grouped by frame in time
 */
function getPlottableFrames() {
    let dataFrames = [];
    _numFrames = 0;
    let orderedPlayerKeys = Object.keys(_playerData).sort((a, b) => a - b);
    for (i=0; i < orderedPlayerKeys.length; i++) {
        let recordTime = orderedPlayerKeys[i];
        // Each frame is a scatter plot of every player & loco
        let dataPlot = {
            x: [_playerData[recordTime][0]], // X
            y: [_playerData[recordTime][2]], // Z
            color: [_colours['Player']]
        };

        for (let carID in _carData) {
            let car = _carData[carID];
            // If there is a record for this car at this time:
            if (car[recordTime]) {
                dataPlot.x.push(car[recordTime][0]); // X
                dataPlot.y.push(car[recordTime][2]); // Z
                let carType = carID.split(_idSep)[1];
                dataPlot.color.push(_colours[carType] ? _colours[carType] : _colours['Mod']);
            }
        }
        dataFrames.push(dataPlot);
    }
    _numFrames = orderedPlayerKeys.length;
    
    return dataFrames;
}

/**
 * Update each point being plotted in the timelapse
 */
function update() {
    console.log(_fps);
    let nextFrame = 1000/_fps;
    if (!_animating) {
        return;
    }
    let data = {
        data: [{
            x: _frames[_frame].x,
            y: _frames[_frame].y,
            marker:{
                size: 10,
                color: _frames[_frame].color
            },
        }]
    };
    let format = {
        transition: {
          duration: 0,//1000/_fps,
          //easing: 'linear',
        },
        frame: {
            duration: nextFrame,
            redraw: false
        }
    };
    Plotly.animate( 'animationPlot', data, format);
    _frame = _frame >= _numFrames - 1 ? 0 : _frame + 1;
    setTimeout(requestAnimationFrame, nextFrame, update);
}

/**
 * Begin animating a timelapse of the objects positions
 */
function animateData() {
    _animating = false;
    $('#timelapseButton').attr('disabled', true);
    $('#plotButton').attr('disabled', false);
    $('#animationPlot').empty()
    $('#fpsOption').removeClass('d-none');
    _frame = 0;

    console.log('Animating data');

    Plotly.newPlot('animationPlot', [{
        x: _frames[0].x,
        y: _frames[0].y,
        mode: 'markers'
    }], {
        xaxis: {
            range: [0, _worldDimentions[0]],
            visible: false,
            showgrid: false
        },
        yaxis: {
            range: [0, _worldDimentions[1]],
            scaleanchor: 'x',
            visible: false,
            showgrid: false
        },
        dragmode: 'select',
    }, {
        scrollZoom: true,
        modeBarButtonsToRemove: ['zoom2d', 'pan2d'] // TODO: Figure out why these cause errors
    });
    
    if ($('#animationPlot').attr('src')) {
        console.log("Adding existing background image");
        addBackgroundImage();
    }

    _animating = true;
    requestAnimationFrame(update);
}

/**
 * Add the set image in the background of the existing plot
 */
function addBackgroundImage() {
    if (!$('#animationPlot').attr('src')) {
        console.warn("No image set");
        return
    }
    let update = {
        images: [
            {
                'source': $('#animationPlot').attr('src'),
                'xref': 'x',
                'yref': 'y',
                'x': 0,
                'y': _worldDimentions[1],
                'sizex': _worldDimentions[0],
                'sizey': _worldDimentions[1],
                'sizing': 'stretch',
                'opacity': 0.6,
                'layer': 'below'
            }
        ]
    };
    Plotly.relayout('animationPlot', update);
}

/**
 * Instead of just putting a local image in the background we have to get the
 * user to import it. Thanks cors
 */
function importBackgroundImage(image) {
    console.log(`Setting ${image.name} as background image`);
    var FR = new FileReader();
    FR.addEventListener('load', function(e) {
        $('#animationPlot').attr('src', e.target.result);
        if ($('#animationPlot').children().length) {
            addBackgroundImage();
        } else {
            alert("Image will render when your traced path (csv) is loaded.");
        }
    });
    FR.readAsDataURL(image);
}

////////////////////////////////////////////////////////////////////////////////
// End // Functions for graphing & animating the data //
////////////////////////////////////////////////////////////////////////////////

/**
 * Return the last item in the given array
 */
function last(array) {
    return array[array.length - 1];
}
