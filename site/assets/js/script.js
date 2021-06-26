document.addEventListener('DOMContentLoaded', function (event) {
    let el = document.getElementById('dip');
    let hummus = document.getElementById('hummus');
    el.addEventListener('click', ev => {
        fetch('/index.json')
            .then(data => data.json())
            .then(json => {
                let randElement = json[Math.floor(Math.random() * json.length)];
                hummus.style.display = "visible";
                hummus.src = randElement.url;
            })
            .catch(reason => {
                hummus.style.display = "none";
            })
    })

});